
using StackExchange.Redis;
using Npgsql;

var redis = ConnectionMultiplexer.Connect("redis");
var db = redis.GetDatabase();

var connString = "Host=db;Username=postgres;Password=password;Database=postgres";

using var sqlConnection = new NpgsqlConnection(connString);
sqlConnection.Open();

while (true)
{
    var vote = db.ListRightPop("votes");
    if (!vote.IsNullOrEmpty)
    {
        using var command = new NpgsqlCommand($"INSERT INTO votes (vote) VALUES ('{vote}')", sqlConnection);
        command.ExecuteNonQuery();
        Console.WriteLine($"Inserted vote for {vote}");
    }
    System.Threading.Thread.Sleep(1000);
}