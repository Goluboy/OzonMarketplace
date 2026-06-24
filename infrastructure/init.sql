SELECT 'CREATE DATABASE products' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'products')\gexec
SELECT 'CREATE DATABASE orders' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'orders')\gexec
SELECT 'CREATE DATABASE keycloak' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec