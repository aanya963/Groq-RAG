/* 
DotNetEnv : Loads environment variables from .env.
Npgsql: Official PostgreSQL driver for .NET.
        Allows C# to talk to PostgreSQL.
        Example operations:
            open connection
            execute SQL
            read results
            
Pgvector : Library to support vector datatype.

- Responsibility of this class:
    Store embeddings
    Search embeddings
*/
using DotNetEnv; 
using Npgsql;
using Pgvector;

namespace RAGDemo.Services
{
    public class VectorDbService
    {
        private readonly string connString;

        // Static constructor to load .env once
        
        //A static constructor runs once when the class loads.
        //This reads the .env file.
        static VectorDbService()
        {
            Env.Load(); // loads .env variables
        }
        // private readonly string connString = "Host=localhost;Username=postgres;Password=admin;Database=ragdb";

        public VectorDbService()
        {
            // Build connection string from .env
            var host = Env.GetString("DB_HOST");
            var user = Env.GetString("DB_USER");
            var pass = Env.GetString("DB_PASS");
            var db = Env.GetString("DB_NAME");
            var port = Env.GetString("DB_PORT", "5432");

            //This will be used whenever we open a DB connection.
            connString = $"Host={host};Port={port};Username={user};Password={pass};Database={db}";
        }
        /*  saves one chunk into the database.
            Input : text → chunk text
                    embedding → vector for that text
            This prepares a PostgreSQL connection configuration.
        */
        public async Task InsertChunk(string text, float[] embedding)
        {
            //Create DB connection builder
            //This prepares a PostgreSQL connection configuration.
            var builder = new NpgsqlDataSourceBuilder(connString);
            //Enable vector support
            builder.UseVector();
            //This creates an actual database connection factory.
            // dataSource = "ready-to-open database connection"
            var dataSource = builder.Build();
            //This connects to PostgreSQL.
            await using var conn = await dataSource.OpenConnectionAsync();
            /*
                This creates a SQL query.
                @content and @embedding are parameters.
                Why parameters?
                Because we will insert dynamic values safely.
            */
            var cmd = new NpgsqlCommand(
                "INSERT INTO documents (content, embedding) VALUES (@content, @embedding)",
                conn
            );
            //Pass the values
            cmd.Parameters.AddWithValue("content", text);
            //we convert the float array into a pgvector object.
            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

            await cmd.ExecuteNonQueryAsync();
        }
        /*
            Input : query embedding, Output: list of relevant chunks
        */
        public async Task<List<string>> SearchSimilar(float[] embedding)
        {
            //Create results lis, This will store the retrieved chunks.
            var results = new List<string>();
            //Create DB connection
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            var dataSource = builder.Build();
            
            //Open connection
            await using var conn = await dataSource.OpenConnectionAsync();
            /*
                SQL QUERY
                Find rows
                    whose embedding vectors
                    are closest to the query vector'
                    here top 3
                <-> means vector distance.
                Smaller distance = more similar meaning.
            */
            var cmd = new NpgsqlCommand(
                "SELECT content FROM documents ORDER BY embedding <-> @embedding LIMIT 3",
                conn
            );
            //Pass query embedding
            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));
            //Executing the SQL query
            //This line runs the SQL query.
            await using var reader = await cmd.ExecuteReaderAsync();
            //Reading each row
            //Extracting the value
            //Reads the first column
            //Adds it to the results list.
            //Move to next row (asynchronously)
            //Take first column from current row
            //You selected only one column. So : Column 0 = content
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }
    }
}