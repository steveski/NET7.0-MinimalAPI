

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<EntityDbContext>(
    options =>
    {
        options.UseInMemoryDatabase("Todos");
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.InferSecuritySchemes();
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            },
            new string[] { }
        }
    });
});
builder.Services.Configure<SwaggerGeneratorOptions>(options =>
{
    options.InferSecuritySchemes = true;
});
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
//app.UseAuthentication();
//app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

// MapGroup() => New.NET 7.0 feature
var todos = app.MapGroup("/todos").RequireAuthorization();

todos.MapGet("/", async (EntityDbContext db) =>
{
   return await db.Todos.ToListAsync();
});

todos.MapPost("/", async (Todo todo, EntityDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    // TypeResult => New .NET 7.0 feature
    return TypedResults.Created($"/todos/{todo.Id}", todo);
});

todos.MapGet("/{id}", async Task<Results<Ok<Todo>, NotFound>> (int id, EntityDbContext db) =>
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(todo)
            : TypedResults.NotFound();
});

// Results<T1, T2> => New .NET 7.0 feature
todos.MapPut("/{id}", async Task<Results<NotFound, NoContent>> (int id, Todo inTodo, EntityDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if(todo is null)
        return TypedResults.NotFound();

    todo.Name = inTodo.Name;
    todo.IsComplete = inTodo.IsComplete;

    await db.SaveChangesAsync();

    return TypedResults.NoContent();
});

todos.MapDelete("/{id}", async Task<Results<NotFound, NoContent>> (int id, EntityDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
        return TypedResults.NotFound();

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
});


app.Run();




public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }

}

public class EntityDbContext : DbContext
{
    public EntityDbContext(DbContextOptions<EntityDbContext> options) : base(options)
    {

    }

    public DbSet<Todo> Todos => Set<Todo>();

}
