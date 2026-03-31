

SELECT COUNT(*) FROM documents;
```
Restart your app and run it again. The number will keep growing.

---

### The Concept: Document Fingerprinting

The fix is simple — before indexing, check if this document has already been indexed.

The professional way to do this is **hashing**: you generate a short unique fingerprint (MD5 or SHA256) of the PDF's content. You store that hash in the database. On startup, you check if the hash already exists. If it does — skip indexing entirely.
```
App starts
    ↓
Hash the PDF content
    ↓
Does this hash exist in DB?
    ├── YES → Skip indexing, go straight to serving API
    └── NO  → Index the PDF, save the hash
============== Improvement 3 ====
postgre sql connection error :
echo 'export PATH="/Library/PostgreSQL/17/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc

1. psql -U postgres
account: postgres
2. Password: admin
3. \c ragdb


username: postgres
password: (your password)
port: 5432

CREATE DATABASE ragdb;
\c ragdb
CREATE EXTENSION vector;

CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    content TEXT,
    embedding VECTOR(1536)
);

//each row stores: text chunk + vector representation


