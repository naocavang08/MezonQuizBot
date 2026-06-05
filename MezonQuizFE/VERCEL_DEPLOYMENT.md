# Vercel Frontend Deployment

This deploys the Vite React frontend to Vercel and points it at the Render backend.

## Vercel project

- Import the GitHub repository into Vercel.
- Set **Root Directory** to `MezonQuizFE`.
- Use the **Vite** framework preset.
- Set **Install Command** to `npm ci`.
- Set **Build Command** to `npm run build`.
- Set **Output Directory** to `dist`.

## Environment variables

Create this variable in the Vercel project for Production and Preview:

```text
VITE_QUIZ_API_URL=https://mezonquiz-api-latest.onrender.com
```

The frontend uses this value for both REST API calls and the SignalR hub. If the variable is not set, local development keeps using relative paths so Vite proxy rules still work.

## Backend production settings

After Vercel creates the production domain, update the backend production configuration stored in the GitHub secret `APPSETTINGS_PRODUCTION`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://<vercel-production-domain>"
    ]
  },
  "MezonOAuth2": {
    "RedirectUri": "https://<vercel-production-domain>/oauth/mezon/callback"
  }
}
```

Keep the rest of the production JSON unchanged. Redeploy the backend after updating the secret.

## Smoke checks

- Open `/explore` and `/quizzes` directly on the Vercel domain.
- Confirm public quiz/category data loads from Render.
- Login with Mezon and confirm the callback returns to `/oauth/mezon/callback` on the Vercel domain.
- Open a quiz session and confirm realtime updates connect through SignalR.
