CREATE EXTENSION IF NOT EXISTS vector;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'vector') THEN
        RAISE NOTICE 'pgvector extension successfully installed!';
    ELSE
        RAISE EXCEPTION 'Failed to install pgvector extension';
    END IF;
END $$;