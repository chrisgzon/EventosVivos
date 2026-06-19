# EventosVivos 🎭

Sistema de reservas para eventos culturales, conferencias y talleres — prueba técnica Fullstack **.NET 10 + Angular 19**.

---

## Estructura del repositorio

```
EventosVivos/
├── eventosvivos-backend/     # API REST — .NET 10 / Clean Architecture
├── eventosvivos-frontend/    # SPA — Angular 19
├── docker-compose.yml        # Orquestación completa (SQL Server + API + Frontend)
└── init.sql                  # Schema SQL Server + datos de referencia
```

Cada proyecto tiene su propio `README.md` con instrucciones detalladas:

- 📦 [`eventosvivos-backend/README.md`](./eventosvivos-backend/README.md) — arquitectura, endpoints, reglas de negocio, tests
- 🖥️ [`eventosvivos-frontend/README.md`](./eventosvivos-frontend/README.md) — estructura, componentes, variables de entorno

---

## Inicio rápido con Docker 🐳

### Prerrequisitos

| Herramienta | Versión mínima |
|-------------|---------------|
| Docker | 20.x |
| Docker Compose | 2.x (incluido en Docker Desktop) |

> No necesitas tener .NET ni Node.js instalados localmente.

### 1. Clonar el repositorio

```bash
git clone https://github.com/chrisgzon/EventosVivos.git
cd EventosVivos
```

### 2. Levantar los servicios

```bash
docker-compose up -d
```

Docker construirá las imágenes y levantará tres servicios:

| Servicio | Descripción | Puerto |
|---|---|---|
| `sqlserver` | SQL Server 2022 Developer Edition | `1434` |
| `api` | .NET 10 Web API | `7262` |
| `frontend` | Angular 19 servido con Nginx | `4200` |

> La primera ejecución puede tardar **2-3 minutos** mientras Docker descarga las imágenes base.

### 3. Inicializar la base de datos

Una vez que el contenedor `sqlserver` esté corriendo, conéctate a la instancia y ejecuta el script `init.sql` manualmente. El script crea la base de datos, todas las tablas, índices y los tres venues de referencia.

**Opción A — copiando el script al contenedor (terminal):**

```bash
# Copiar init.sql dentro del contenedor
docker cp init.sql eventosvivos_db:/init.sql

# Ejecutar el script
docker exec eventosvivos_db \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost:1434 -U sa -P "EventosVivos_2026!" \
  -i /init.sql -No -C
```

**Opción B — con SSMS o DBeaver (recomendado):**

1. Conectarse con las credenciales de la sección [Credenciales de la base de datos](#credenciales-de-la-base-de-datos).
2. Abrir el archivo `init.sql` de la raíz del repositorio.
3. Ejecutar el script completo (`F5` en SSMS).

### 4. Verificar que todo está corriendo

```bash
docker-compose ps
```

Los tres servicios deben mostrar estado `running`.

### 5. Acceder a la aplicación

| URL | Descripción |
|-----|-------------|
| `http://localhost:4200` | Aplicación Angular |
| `http://localhost:7262/swagger` | Swagger UI — documentación interactiva de la API |
| `http://localhost:7262/api/venues` | Health-check rápido de la API |

---

## Comandos útiles

```bash
# Ver logs en tiempo real
docker-compose logs -f

# Ver logs solo de la API
docker-compose logs -f api

# Detener todos los servicios (conserva los datos)
docker-compose stop

# Detener Y eliminar volúmenes (borra la base de datos)
docker-compose down -v

# Reconstruir imágenes después de cambios en el código
docker-compose up -d --build
```

---

## Credenciales de la base de datos

| Campo | Valor |
|-------|-------|
| Servidor | `localhost,1434` |
| Usuario | `sa` |
| Contraseña | `EventosVivos_2026!` |
| Base de datos | `EventosVivos` |

---

## Tecnologías

| Capa | Stack |
|------|-------|
| Backend | .NET 10 · ASP.NET Core · EF Core 10 · SQL Server 2022 |
| Frontend | Angular 19 · TypeScript · SCSS |
| Tests | xUnit · Moq · FluentAssertions |
| Infraestructura | Docker · Docker Compose · Nginx |

## 🧪 Reporte de Cobertura

👉 [Ver reporte completo](https://chrisgzon.github.io/EventosVivos/eventosvivos-backend/coverage-report/)
