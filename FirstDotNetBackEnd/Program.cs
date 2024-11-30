using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITodoService>(new InMemoryTodoService()); //registering the service
// web application builder provides a way to configure the application
// and create a web application instance


var app = builder.Build();
// build the web application instance

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
// UseRewriter method is used to add a URL rewrite middleware to the application

// context - current http reqest and responce 
// next - next middleware in the pipeline
app.Use(async (context, next) =>
{
    Console.WriteLine($"[{context.Request.Method}] {context.Request.Path}");
    await next(context);
    Console.WriteLine($"Response status code: {context.Response.StatusCode}");
});


app.MapGet("/", () => "Hello World!");
// MapGet method is used to map a GET request to a specific route


var todos = new List<Todo>
{
    new Todo(1, "Learn C#", DateTime.Now.AddDays(1), false),
    new Todo(2, "Build awesome apps", DateTime.Now.AddDays(2), false),
    new Todo(3, "Contribute to OSS", DateTime.Now.AddDays(3), false)
};

// Minimal APIs

// crud apis for todos
app.MapPost("/todos", (Todo todo, ITodoService service) =>
{
    service.AddTodo(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
}) //end point filter used to validate the input
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate < DateTime.Now)
    {
        errors.Add("DueDate", new[] { "Due date must be in the future" });
    }

    if (taskArgument.Name.Length < 3)
    {
        errors.Add("Name", new[] { "Name must be at least 3 characters long" });
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors); //400 code + erros string 
        
    }
    return await next(context);
});
    
    
    
app.MapGet("/todos", (ITodoService service) => service.GetTodos());

//  This can return either Ok With Todo or NotFound response
app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITodoService service) =>
{
    var todo = service.GetTodoById(id);
    return todo is null ? TypedResults.NotFound() : TypedResults.Ok(todo);
});

app.MapDelete("/todos/{id}", (int id, ITodoService service) =>
{
    service.DeleteTodoById(id);
    return Results.NoContent();
});

app.Run();
// Run the application

public record Todo(int Id, string Name, DateTime DueDate, bool IsComplete);

interface ITodoService
{
    List<Todo> GetTodos();
    Todo GetTodoById(int id);
    void AddTodo(Todo todo);
    void DeleteTodoById(int id);
}

class InMemoryTodoService : ITodoService
{
    private List<Todo> todos = new()
    {
        new Todo(1, "Learn C#", DateTime.Now.AddDays(1), false),
        new Todo(2, "Build awesome apps", DateTime.Now.AddDays(2), false),
        new Todo(3, "Contribute to OSS", DateTime.Now.AddDays(3), false)
    };

    public List<Todo> GetTodos() => todos;

    public Todo GetTodoById(int id) => todos.FirstOrDefault(x => x.Id == id);

    public void AddTodo(Todo todo) => todos.Add(todo);

    public void DeleteTodoById(int id) => todos.RemoveAll(x => x.Id == id);
    
}


