using DotNetEnv;
using Npgsql;
using Pgvector;

namespace RAGDemo.Services
{
    public class VectorDbService
    {
        // Built ONCE, reused forever
        private readonly NpgsqlDataSource _dataSource;

        static VectorDbService()
        {
            Env.Load();
        }

        public VectorDbService()
        {
            var host = Env.GetString("DB_HOST");
            var user = Env.GetString("DB_USER");
            var pass = Env.GetString("DB_PASS");
            var db   = Env.GetString("DB_NAME");
            var port = Env.GetString("DB_PORT", "5432");

            var connString = $"Host={host};Port={port};Username={user};Password={pass};Database={db}";

            // Build the data source once here in the constructor
            var builder = new NpgsqlDataSourceBuilder(connString);
            builder.UseVector();
            _dataSource = builder.Build();
        }

        public async Task InsertChunk(string text, float[] embedding)
        {
            // Just borrow a connection from the pool
            await using var conn = await _dataSource.OpenConnectionAsync();

            var cmd = new NpgsqlCommand(
                "INSERT INTO documents (content, embedding) VALUES (@content, @embedding)",
                conn
            );
            cmd.Parameters.AddWithValue("content", text);
            cmd.Parameters.AddWithValue("embedding", new Vector(embedding));
            await cmd.ExecuteNonQueryAsync();
        }

       public async Task<List<string>> SearchSimilar(float[] embedding, int topK = 5, float threshold = 0.3f)
       {
            var results = new List<string>();

            await using var conn = await _dataSource.OpenConnectionAsync();

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
                if (similarity >= threshold)
                    results.Add(reader.GetString(0));
            }

            return results;
        }

        public async Task<bool> IsAlreadyIndexed(string fileHash)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

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
            await using var conn = await _dataSource.OpenConnectionAsync();

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