# Render Backend Deployment

This deploys only the ASP.NET Core backend. The frontend can stay local or be hosted elsewhere.

## Render service

- Create a Render PostgreSQL instance first.
- Create a Render Web Service from this repository.
- Set **Root Directory** to `MezonQuiz`.
- Set **Environment** to `Docker`.
- Set **Dockerfile Path** to `Dockerfile`.
- Set **Health Check Path** to `/`.
- Disable Render auto-deploy if GitHub Actions should be the only deployment trigger.

## Environment variables

Copy the keys from `.env.render.example` into Render Environment Variables and replace every placeholder.

Use Render's PostgreSQL connection string for `ConnectionStrings__DefaultConnection`. Keep all secrets in Render; do not copy production secrets into `appsettings.json` or into the Docker image.

## Frontend configuration

Point the frontend to the Render backend URL:

```text
VITE_QUIZ_API_URL=https://<render-service-name>.onrender.com
```

Update the Mezon OAuth callback URL to:

```text
https://<render-service-name>.onrender.com/oauth/mezon/callback
```

## Local Docker check

Create a local env file from `.env.render.example`, fill it with non-production values, then run:

```bash
docker build -t mezonquiz-api .
docker run --rm -p 8080:8080 --env-file .env.local mezonquiz-api
```

On startup, the backend runs EF migrations and then seed data.

## GitHub Actions deployment

The workflow `.github/workflows/deploy-backend-render.yml` verifies the backend and then triggers Render.

In Render, open the backend Web Service and create a **Deploy Hook**. Add that URL to GitHub:

```text
Settings > Secrets and variables > Actions > New repository secret
Name: RENDER_DEPLOY_HOOK_URL
Value: <Render deploy hook URL>
```

The workflow behavior is:

- Pull requests that touch `MezonQuiz/**` run tests and Docker build only.
- Pushes to `main` or `master` run tests, Docker build, then trigger Render deploy.
- Manual runs through `workflow_dispatch` also trigger Render after verification.
