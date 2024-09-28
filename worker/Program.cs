using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
// Define una política de CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Configurar PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
builder.Services.AddDbContext<VotingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configurar Redis
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisConnection);
IDatabase redisDb = redis.GetDatabase();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Deshabilitar la redirección HTTPS en desarrollo
    app.Use((context, next) =>
    {
        context.Request.Scheme = "http";
        return next();
    });
}
// Habilitar CORS antes de otros middlewares que usan CORS (como Autorización)
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();

app.MapPost("/vote", async (VotingDbContext dbContext, string animal) =>
{
    if (string.IsNullOrEmpty(animal))
    {
        return Results.BadRequest("Animal name is required.");
    }

    // Guardar en PostgreSQL
    var vote = new Vote { Animal = animal, CreatedAt = DateTime.UtcNow };
    await dbContext.Votes.AddAsync(vote);
    await dbContext.SaveChangesAsync();

    // Guardar en Redis (clave: "animal-vote-{animal}")
    await redisDb.StringIncrementAsync($"animal-vote-{animal}");

    return Results.Ok($"Vote for {animal} has been saved.");
});

// Endpoint para obtener los votos desde PostgreSQL y Redis
app.MapGet("/votes", async (VotingDbContext dbContext) =>
{
    // Obtener todos los votos desde PostgreSQL
    var votesFromDb = await dbContext.Votes
        .GroupBy(v => v.Animal)
        .Select(g => new { Animal = g.Key, Count = g.Count() })
        .ToListAsync();

    // Obtener votos de Redis
    var redisKeys = redis.GetServer(redisConnection).Keys(pattern: "animal-vote-*");
    var votesFromRedis = redisKeys.ToDictionary(
        key => key.ToString().Replace("animal-vote-", ""),
        key => (int)redisDb.StringGet(key)
    );

    // Combinar resultados de PostgreSQL y Redis
    var combinedVotes = votesFromDb.Select(voteDb => new
    {
        Animal = voteDb.Animal,
        CountFromDb = voteDb.Count,
        CountFromRedis = votesFromRedis.ContainsKey(voteDb.Animal) ? votesFromRedis[voteDb.Animal] : 0
    });

    return Results.Ok(combinedVotes);
});

app.Run();

// Modelo de voto
public class Vote
{
    [Key]
    public int Id { get; set; }
    public string Animal { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Contexto de base de datos
public class VotingDbContext : DbContext
{
    public VotingDbContext(DbContextOptions<VotingDbContext> options) : base(options) { }
    public DbSet<Vote> Votes => Set<Vote>();
}
