# SchemeVault UI screenshots

Captured from the Expo web app against the running API and the seeded demo tenant **Northern Plant Hire Ltd**.

Demo sign-in: `demo@schemevault.test` / `DemoPassw0rd!`

Viewport: iPhone-sized (390×844 @2x). Files are WebP.

| File | Screen |
| --- | --- |
| [01-login.webp](./01-login.webp) | Sign in (demo credentials pre-filled) |
| [02-dashboard.webp](./02-dashboard.webp) | Home dashboard — renewal traffic lights, due/expired counts, missing evidence |
| [03-schemes.webp](./03-schemes.webp) | Scheme catalogue with SSIP badges and per-scheme gap counts |
| [04-vault.webp](./04-vault.webp) | Evidence vault (RAMS, H&S policy, EL insurance) |
| [05-renewals.webp](./05-renewals.webp) | Accreditation records (SafeContractor red, SMAS/CHAS amber, Constructionline green) |
| [06-settings.webp](./06-settings.webp) | Organisation name, role, sign out |

These are product screenshots of the live MVP, not mock-ups. Expiry copy in the shots reflects the seed data relative to **19 Sep 2026**.

To regenerate locally:

```bash
cd apps/api && dotnet run
# in another terminal
cd apps/mobile && EXPO_PUBLIC_API_URL=http://127.0.0.1:5080 npx expo start --web --port 8081
# then sign in as the demo user and capture the six tabs
```
