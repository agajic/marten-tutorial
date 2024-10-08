using Marten;
using Marten.Events.Aggregation;
using Marten.Events.Projections;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMarten(
    o =>
    {
        o.Connection(builder.Configuration.GetConnectionString("DefaultConnection"));

        o.Projections.Add<Projector>(ProjectionLifecycle.Inline);
        o.Projections.Snapshot<P3>(SnapshotLifecycle.Async);
    });

var app = builder.Build();

app.MapGet("/", () => "Hello World");

app.MapGet("/p1", (IQuerySession session) =>  //for reading from the database, can't do writing
{
    return session.Events.AggregateStreamAsync<P1>(new Guid("01926c57-33f1-42c1-a536-f09e2f25bf35"));
});

app.MapGet("/p2", (IQuerySession session) =>  //for reading from the database, can't do writing
{
    return session.Events.AggregateStreamAsync<P2>(new Guid("01926c57-33f1-42c1-a536-f09e2f25bf35"));
});

app.MapGet("/p3", (IQuerySession session) =>  //for reading from the database, can't do writing
{
    return session.LoadAsync<P3>(new Guid("01926c57-33f1-42c1-a536-f09e2f25bf35"));
});

app.MapGet("/cart", (IDocumentSession session) =>
{
    var result = session.Events.StartStream(
        new AddedToCart("Pants", 2),
        new UpdatedShippingInformation("Adresa123", "0638765298")
        );

    session.SaveChangesAsync();

    return "created!";
});

app.MapGet("/append", (IDocumentSession session) =>
{
    session.Events.Append(
        new Guid("01926c57-33f1-42c1-a536-f09e2f25bf35"),
        new AddedToCart("T-Shirt", 2)
        );

    session.SaveChangesAsync();

    return "created!";
});

app.MapGet("/rebuild", async (IDocumentStore store) =>
{
    //var daemon = store.BuildProjectionDaemonAsync();
    await store.Advanced.RebuildSingleStreamAsync<P3>(new Guid("01926c57-33f1-42c1-a536-f09e2f25bf35"));
    return "rebuild";
});

app.Run();



public class P1
{
    public Guid Id { get; set; }
    public List<string> Products { get; set; } = new();

    public void Apply(AddedToCart e)
    {
        Products.Add(e.Name);
    }
}

public class P2
{
    public Guid Id { get; set; }
    public int TotalQty { get; set; }
}

public class P3
{
    public Guid Id { get; set; }
    public List<string> Products { get; set; } = new();
    public string PhoneNumber { get; set; }

    public void Apply(AddedToCart e)
    {
        Products.Add(e.Name);
    }
    public void Apply(UpdatedShippingInformation e)
    {
        PhoneNumber = e.PhoneNumber;
    }
}


public class Projector : SingleStreamProjection<P2>
{
    public void Apply(P2 snapshot, AddedToCart e)
    {
        snapshot.TotalQty += e.Qty;
    }
}

public record AddedToCart(string Name, int Qty);
public record UpdatedShippingInformation(string Address, string PhoneNumber);

