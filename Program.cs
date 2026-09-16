using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    service = "AstroBookings",
    status = "ok",
    documentation = "/swagger",
    endpoints = new[] { "/health", "/api/rockets", "/api/launches", "/api/customers", "/api/bookings" }
}));

var rockets = new List<Rocket>
{
    new(1, "Atlas V", 120, 25000),
    new(2, "Falcon 9", 180, 22000),
    new(3, "Starship", 260, 45000)
};

var launches = new List<Launch>
{
    new(1, 2, "Falcon 9", DateTimeOffset.UtcNow.AddDays(8), 240m, 120, "scheduled"),
    new(2, 3, "Starship", DateTimeOffset.UtcNow.AddDays(16), 340m, 180, "scheduled")
};

var customers = new List<Customer>
{
    new(1, "Ana Torres", "ana@astrobookings.com", "555-1001", "Orbital Tours"),
    new(2, "Luis Vega", "luis@astrobookings.com", "555-1002", "Launchers Club")
};

var bookings = new List<Booking>();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "AstroBookings" }));

app.MapGet("/api/rockets", () => Results.Ok(rockets));

app.MapPost("/api/rockets", ([FromBody] CreateRocketRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Rocket name is required." });
    }

    var rocket = new Rocket(rockets.Count + 1, request.Name, request.Capacity, request.MaxRangeKm);
    rockets.Add(rocket);
    return Results.Created($"/api/rockets/{rocket.Id}", rocket);
});

app.MapPut("/api/rockets/{id:int}", (int id, [FromBody] UpdateRocketRequest request) =>
{
    var rocket = rockets.FirstOrDefault(r => r.Id == id);
    if (rocket is null)
    {
        return Results.NotFound();
    }

    rocket.Name = request.Name;
    rocket.Capacity = request.Capacity;
    rocket.MaxRangeKm = request.MaxRangeKm;
    return Results.Ok(rocket);
});

app.MapDelete("/api/rockets/{id:int}", (int id) =>
{
    var rocket = rockets.FirstOrDefault(r => r.Id == id);
    if (rocket is null)
    {
        return Results.NotFound();
    }

    rockets.Remove(rocket);
    return Results.NoContent();
});

app.MapGet("/api/launches", () => Results.Ok(launches.Select(MapLaunchResponse)));

app.MapGet("/api/launches/{launchId:int}/availability", (int launchId) =>
{
    var launch = launches.FirstOrDefault(l => l.Id == launchId);
    if (launch is null)
    {
        return Results.NotFound();
    }

    var bookedSeats = bookings
        .Where(b => b.LaunchId == launchId)
        .Sum(b => b.Seats);

    var availability = new
    {
        launchId,
        launch.RocketName,
        launch.TotalSeats,
        bookedSeats,
        availableSeats = launch.TotalSeats - bookedSeats
    };

    return Results.Ok(availability);
});

app.MapPost("/api/launches", ([FromBody] CreateLaunchRequest request) =>
{
    if (request.Price <= 0)
    {
        return Results.BadRequest(new { message = "Launch price must be greater than zero." });
    }

    if (request.TotalSeats <= 0)
    {
        return Results.BadRequest(new { message = "Launch total seats must be greater than zero." });
    }

    if (request.Date <= DateTimeOffset.UtcNow)
    {
        return Results.BadRequest(new { message = "Launch date must be in the future." });
    }

    var rocket = rockets.FirstOrDefault(r => r.Id == request.RocketId);
    if (rocket is null)
    {
        return Results.BadRequest(new { message = "Rocket not found." });
    }

    var launch = new Launch(
        launches.Count + 1,
        request.RocketId,
        rocket.Name,
        request.Date,
        request.Price,
        request.TotalSeats,
        request.Status ?? "scheduled");

    launches.Add(launch);
    return Results.Created($"/api/launches/{launch.Id}", MapLaunchResponse(launch));
});

app.MapPut("/api/launches/{launchId:int}", (int launchId, [FromBody] UpdateLaunchRequest request) =>
{
    var launch = launches.FirstOrDefault(l => l.Id == launchId);
    if (launch is null)
    {
        return Results.NotFound();
    }

    var rocket = rockets.FirstOrDefault(r => r.Id == request.RocketId);
    if (rocket is null)
    {
        return Results.BadRequest(new { message = "Rocket not found." });
    }

    launch.RocketId = request.RocketId;
    launch.RocketName = rocket.Name;
    launch.Date = request.Date;
    launch.Price = request.Price;
    launch.TotalSeats = request.TotalSeats;
    launch.Status = request.Status;

    return Results.Ok(MapLaunchResponse(launch));
});

app.MapDelete("/api/launches/{launchId:int}", (int launchId) =>
{
    var launch = launches.FirstOrDefault(l => l.Id == launchId);
    if (launch is null)
    {
        return Results.NotFound();
    }

    launches.Remove(launch);
    return Results.NoContent();
});

app.MapGet("/api/customers", () => Results.Ok(customers));

app.MapPost("/api/customers", ([FromBody] CreateCustomerRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Name and email are required." });
    }

    if (customers.Any(c => c.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.BadRequest(new { message = "Customer email already exists." });
    }

    var customer = new Customer(customers.Count + 1, request.Name, request.Email, request.Phone, request.StoreName);
    customers.Add(customer);
    return Results.Created($"/api/customers/{customer.Id}", customer);
});

app.MapPut("/api/customers/{id:int}", (int id, [FromBody] UpdateCustomerRequest request) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer is null)
    {
        return Results.NotFound();
    }

    if (!string.IsNullOrWhiteSpace(request.Email) && !customer.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Email cannot be changed after creation." });
    }

    customer.Name = request.Name ?? customer.Name;
    customer.Phone = request.Phone ?? customer.Phone;
    customer.StoreName = request.StoreName ?? customer.StoreName;
    return Results.Ok(customer);
});

app.MapDelete("/api/customers/{id:int}", (int id) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer is null)
    {
        return Results.NotFound();
    }

    customers.Remove(customer);
    return Results.NoContent();
});

app.MapPost("/api/bookings", ([FromBody] CreateBookingRequest request) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == request.CustomerId);
    if (customer is null)
    {
        return Results.BadRequest(new { message = "Customer not found." });
    }

    var launch = launches.FirstOrDefault(l => l.Id == request.LaunchId);
    if (launch is null)
    {
        return Results.BadRequest(new { message = "Launch not found." });
    }

    if (!launch.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && !launch.Status.Equals("scheduled", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Launch is not accepting bookings." });
    }

    var bookedAlready = bookings.Where(b => b.LaunchId == request.LaunchId).Sum(b => b.Seats);
    if (bookedAlready + request.Seats > launch.TotalSeats)
    {
        return Results.BadRequest(new { message = "Not enough seats left for this launch." });
    }

    var booking = new Booking(
        bookings.Count + 1,
        request.CustomerId,
        request.LaunchId,
        request.Seats,
        request.Seats * launch.Price,
        "pending");

    bookings.Add(booking);
    return Results.Created($"/api/bookings/{booking.Id}", booking);
});

app.MapGet("/api/bookings/{id:int}", (int id) =>
{
    var booking = bookings.FirstOrDefault(b => b.Id == id);
    return booking is null ? Results.NotFound() : Results.Ok(booking);
});

app.MapGet("/api/bookings/launch/{launchId:int}", (int launchId) => Results.Ok(bookings.Where(b => b.LaunchId == launchId)));
app.MapGet("/api/bookings/customer/{email}", (string email) => Results.Ok(bookings.Where(b => customers.Any(c => c.Id == b.CustomerId && c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))));

app.Run();

LaunchResponse MapLaunchResponse(Launch launch)
{
    var bookedSeats = bookingsForLaunch(launch.Id);
    return new LaunchResponse(
        launch.Id,
        launch.RocketId,
        launch.RocketName,
        launch.Date,
        launch.Price,
        launch.TotalSeats,
        bookedSeats,
        launch.TotalSeats - bookedSeats,
        launch.Status);
}

int bookingsForLaunch(int launchId) => bookings.Count(b => b.LaunchId == launchId);

class Rocket
{
    public Rocket(int id, string name, int capacity, int maxRangeKm)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
        MaxRangeKm = maxRangeKm;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public int Capacity { get; set; }
    public int MaxRangeKm { get; set; }
}

class Launch
{
    public Launch(int id, int rocketId, string rocketName, DateTimeOffset date, decimal price, int totalSeats, string status)
    {
        Id = id;
        RocketId = rocketId;
        RocketName = rocketName;
        Date = date;
        Price = price;
        TotalSeats = totalSeats;
        Status = status;
    }

    public int Id { get; set; }
    public int RocketId { get; set; }
    public string RocketName { get; set; }
    public DateTimeOffset Date { get; set; }
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
    public string Status { get; set; }
}

record LaunchResponse(int Id, int RocketId, string RocketName, DateTimeOffset Date, decimal Price, int TotalSeats, int BookedSeats, int AvailableSeats, string Status);

class Customer
{
    public Customer(int id, string name, string email, string phone, string storeName)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        StoreName = storeName;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string StoreName { get; set; }
}

class Booking
{
    public Booking(int id, int customerId, int launchId, int seats, decimal totalPrice, string paymentStatus)
    {
        Id = id;
        CustomerId = customerId;
        LaunchId = launchId;
        Seats = seats;
        TotalPrice = totalPrice;
        PaymentStatus = paymentStatus;
    }

    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int LaunchId { get; set; }
    public int Seats { get; set; }
    public decimal TotalPrice { get; set; }
    public string PaymentStatus { get; set; }
}

record CreateRocketRequest(string Name, int Capacity, int MaxRangeKm);
record UpdateRocketRequest(string Name, int Capacity, int MaxRangeKm);
record CreateLaunchRequest(int RocketId, DateTimeOffset Date, decimal Price, int TotalSeats, string? Status);
record UpdateLaunchRequest(int RocketId, DateTimeOffset Date, decimal Price, int TotalSeats, string Status);
record CreateCustomerRequest(string Name, string Email, string Phone, string StoreName);
record UpdateCustomerRequest(string? Name, string? Phone, string? StoreName, string? Email);
record CreateBookingRequest(int CustomerId, int LaunchId, int Seats);
