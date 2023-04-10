using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.ResolveConflictingActions(x => x.Last()));
builder.Services.AddApiVersioning(setup =>
{
    setup.DefaultApiVersion = new ApiVersion(1, 0);
    setup.AssumeDefaultVersionWhenUnspecified = true;
    setup.ReportApiVersions = true;
    setup.ApiVersionReader = new MediaTypeApiVersionReader("X-Version");

});
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

var versionSet = app.NewApiVersionSet()
                    .HasApiVersion(new ApiVersion(1, 0))
                    .HasApiVersion(new ApiVersion(1, 1))
                    .HasDeprecatedApiVersion(new ApiVersion(1, 0))
                    .Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region endpoints
var todoItems = app.MapGroup("/todoitems");

todoItems.MapGet("/", async (TodoDb db) =>
    await db.Todos
        .ToListAsync())
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0));

todoItems.MapGet("/complete", async (TodoDb db) =>
    await db.Todos
        .Where(t => t.IsComplete)
            .ToListAsync())
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0));

todoItems.MapGet("/{id}", async (int id, TodoDb db) =>
    await db.Todos
        .FindAsync(id)
            is Todo todo
                ? Results.Ok(todo)
                : Results.NotFound())
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0)); ;

todoItems.MapPost("/", async (Todo todo, TodoDb db) =>
{
    db.Todos
        .Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0));

todoItems.MapPut("/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0));

todoItems.MapDelete("/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0));
#endregion



app.Run();

#region data
class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
#endregion
