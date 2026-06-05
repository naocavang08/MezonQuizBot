# Render Backend Deployment

This deploys only the ASP.NET Core backend. The frontend can stay local or be hosted elsewhere.

## Render service

- Create a Render PostgreSQL instance first.
- Create a Render Web Service from an existing Docker image.
- Set the image to `docker.io/<dockerhub-username>/mezonquiz-api:latest`.
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

The workflow `.github/workflows/deploy-backend-render.yml` verifies the backend, pushes the Docker image to Docker Hub, and then triggers Render.

Create a Docker Hub access token, then add these GitHub repository secrets:

```text
Settings > Secrets and variables > Actions > New repository secret

Name: DOCKERHUB_USERNAME
Value: <Docker Hub username>

Name: DOCKERHUB_TOKEN
Value: <Docker Hub access token>
```

In Render, open the backend Web Service and create a **Deploy Hook**. Add that URL to GitHub:

```text
Name: DEPLOY_WEBHOOK_URL
Value: <Render deploy hook URL>
```

The workflow behavior is:

- Pull requests that touch `MezonQuiz/**` run tests and Docker build only.
- Pushes to `main` or `master` run tests, push `latest` and commit-SHA tags to Docker Hub, then trigger Render deploy.
- Manual runs through `workflow_dispatch` do the same after verification.
