# TASK-01 · Project Structure Setup

## Goal

Establish the roo-level directory structure so both the frontend and the backend can coexist cleanly, share tooling config and able to run together in development.

## Directory Layout
```
poc-data-validation/
├── ClientAdmin/                    # Vite + React SPA
├── POC.CustomerValidation/         # C# ASP.NET Core Web API
├── POC.Database/                   # SQL Server database project
├── TASK-FILES/
├── claude.md                       # claude file which points to TASK-FILES directory
└── README.md
```


## Steps
1. Create 'ClientAdmin/' and 'POC.CustomerValidation directories.
2. Create a root '.gitignore' that exludes 'node_modules/', 'bin/', 'obj/', '.env', '*.db', '*db-slm', '*.db-wal'.
3. Create a root 'README.md' documentation how to run both projects in development.
4. Create a root 'docker-compose.yml' (optional/stretch) with services for 'frontend' and 'backend' for easy local startup.
5. Document the environment variables each service needs (see )