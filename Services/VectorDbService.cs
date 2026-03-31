using DotNetEnv; 
using Npgsql;
using Pgvector;

namespace RAGDemo.Services
{
    public class VectorDbService
    {
        private readonly string connString;
        static VectorDbService()
        {
            Env.Load(); // loads .env variables
        }
        public VectorDbService()
        {
            var host = Env.GetString("DB_HOST");
            var user = Env.GetString("DB_USER");
            var pass = Env.GetString("DB_PASS");
            var db = Env.GetString("DB_NAME");
            var port = Env.GetString("DB_PORT", "5432");

            connString = $"Host={host};Port={port};Username={user};Password={pass};Database={db}";
        }
        public async Task InsertChunk(string text, float[] embedding)
        {
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            var dataSource = builder.Build();
            //connects to PostgreSQL.
            await using var conn = await dataSource.OpenConnectionAsync();
            var cmd = new NpgsqlCommand(
                "INSERT INTO documents (content, embedding) VALUES (@content, @embedding)",
                conn
            );
            cmd.Parameters.AddWithValue("content", text);
            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task<List<string>> SearchSimilar(float[] embedding, int topK = 5, float threshold = 0.5f)
        {
            var results = new List<string>();
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            var dataSource = builder.Build();

            await using var conn = await dataSource.OpenConnectionAsync();

            // 1 - cosine_distance gives us cosine SIMILARITY (higher = more similar)
            var cmd = new NpgsqlCommand(
                @"SELECT content, 1 - (embedding <=> @embedding) AS similarity
                FROM documents
                ORDER BY embedding <=> @embedding
                LIMIT @topK",
                conn
            );

            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));
            cmd.Parameters.AddWithValue("topK", topK);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var similarity = reader.GetDouble(1);

                // Only include chunks that are actually relevant
                if (similarity >= threshold)
                {
                    results.Add(reader.GetString(0));
                }
            }

            return results;
        }
        public async Task<bool> IsAlreadyIndexed(string fileHash)
        {
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            var dataSource = builder.Build();

            await using var conn = await dataSource.OpenConnectionAsync();
            var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM indexed_documents WHERE file_hash = @hash",
                conn
            );
            cmd.Parameters.AddWithValue("hash", fileHash);
            var count = (long)(await cmd.ExecuteScalarAsync())!;
            return count > 0;
        }

        public async Task MarkAsIndexed(string fileHash, string fileName)
        {
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            var dataSource = builder.Build();

            await using var conn = await dataSource.OpenConnectionAsync();
            var cmd = new NpgsqlCommand(
                "INSERT INTO indexed_documents (file_hash, file_name) VALUES (@hash, @name)",
                conn
            );
            cmd.Parameters.AddWithValue("hash", fileHash);
            cmd.Parameters.AddWithValue("name", fileName);
            await cmd.ExecuteNonQueryAsync();
        }
    
    }
}