# AI providers (Create with AI)

Code: `src/EasyPixel.Core/Ai/Providers.cs` (interfaces, `AiProviders` registry) and `src/EasyPixel.Core/Ai/Providers/`.
Checked against the providers' own docs on **2026-11-08**. Providers return raw image bytes; decoding (PNG, and
JPEG/WebP through Avalonia's `Bitmap`) happens in the app layer (`ImageDecode`, Task 5). PNG is requested where the API
allows it; Cloudflare flux answers JPEG.

**VERIFIED** = matches the provider's documentation on that date (link given). **NOT VERIFIED** = docs could not be
reached or don't say; written from the best available knowledge — check these first if a provider starts failing.

## Image generators (order in `AiProviders.Images`: free first)

### `cloudflare` — free with an account — VERIFIED
https://developers.cloudflare.com/workers-ai/models/flux-1-schnell/
`POST https://api.cloudflare.com/client/v4/accounts/{account}/ai/run/@cf/black-forest-labs/flux-1-schnell`,
`Authorization: Bearer {token}`, body `{prompt (1–2048 chars), steps: 4 (≤ 8), seed}`. No width/height (not documented,
not sent). Answer: base64 `image` (JPEG) — the REST envelope `result.image` is inferred; a top-level `image` is also read.

### `pollinations` — free with a key — VERIFIED (route, model, auth); NOT VERIFIED (size/seed params)
https://enter.pollinations.ai (keys: https://enter.pollinations.ai/keys)
`GET https://gen.pollinations.ai/image/{urlencoded prompt}?model=black-forest-labs/flux.1-schnell&width=512&height=512&seed=`,
`Authorization: Bearer sk_…` (`pk_` keys get 401 since 2026-11-01). Answer: image bytes (JPEG/PNG). The prompt is capped
at 600 characters to keep the URL short. `width`/`height`/`seed` are NOT VERIFIED (sent; assumed harmless).

### `openai` — paid — NOT VERIFIED
Docs unreachable on 2026-11-08. `POST https://api.openai.com/v1/images/generations`, Bearer,
`{model:"gpt-image-1", prompt, size:"1024x1024", quality:"low", output_format:"png", n:1}` → `data[0].b64_json`. No seed.

### `gemini` — paid (no free tier for image models) — VERIFIED
https://ai.google.dev/gemini-api/docs/image-generation (Interactions API)
`POST https://generativelanguage.googleapis.com/v1beta/interactions`, headers `x-goog-api-key`, `Api-Revision: 2026-05-20`,
body `{"model":"gemini-nano-banana-2.1","input":"<prompt>","response_format":{"type":"image","mime_type":"image/png","aspect_ratio":"1:1"}}`
→ base64 at `output_image.data`. Other ids: `gemini-3.1-flash-image`, `gemini-2.5-flash-image` (legacy). No seed field.

### `retrodiffusion` — paid, pixel-art specialist — VERIFIED
https://retrodiffusion.ai/app/guide/api, https://github.com/Retro-Diffusion/api-examples
`POST https://api.retrodiffusion.ai/v2/inferences`, header `X-RD-Token: rdpk-…`,
`{prompt, prompt_style:"rd_fast__default", width:128, height:128 (RD Fast 64–384), num_images:1, seed}` →
`{"status":"accepted","task_id"}`; then `GET /v2/inferences/tasks/{task_id}` polled (1 s, ×1.5 up to 5 s, all inside the
90 s timeout, cancel honoured) until `result.base64_images[]` (PNG). A `failed`/`error` status is a friendly error.
The exact "done" status word isn't relied on: the presence of `base64_images` means done.

### Cloudflare key format
EasyPixel stores Cloudflare as ONE key string: `accountId:apiToken`. The Account ID is on the Workers AI dashboard; the
token needs the "Workers AI" permission. A key without `:` is refused with a friendly message before any request.

## Text AIs (translation to English)

| Id | Free | Request → answer | Status |
|---|---|---|---|
| `cloudflare` | yes | same `/ai/run/` route, model `@cf/meta/llama-3.1-8b-instruct-fp8`, `{messages:[system,user]}` → `result.response` | VERIFIED (Workers AI model catalog; llama-3.2-3b is deprecated 2026-11-20) |
| `gemini` | free tier with key | `models/gemini-2.5-flash:generateContent`, `systemInstruction` + `contents` → `candidates[0].content.parts[].text` | NOT VERIFIED (model id and generateContent shape) |
| `openai` | paid | `POST /v1/chat/completions`, `gpt-4o-mini` → `choices[0].message.content` | NOT VERIFIED |

## Sign-up and limits
- Cloudflare Workers AI: https://dash.cloudflare.com/sign-up — 10,000 neurons/day free (reset 00:00 UTC); flux-1-schnell ≈ 43 neurons per 512² 4-step image (~200/day).
- Pollinations: https://enter.pollinations.ai/keys — `sk_` secret keys; free flux on the starter tier reported, not verified.
- OpenAI: https://platform.openai.com/api-keys — paid per image.
- Google AI Studio: https://aistudio.google.com/apikey — text free tier; image ≈ $0.039/image.
- Retro Diffusion: https://retrodiffusion.ai — ≈ $0.01–0.18/image, 10 requests/min/key.

## Not shipped
- Keyless generators: legacy `image.pollinations.ai` watermarks then returns 402; `gen.pollinations.ai` needs a key.
- Hugging Face: free monthly credits removed (Oct 2026).
- Local (Windows + AMD): stable-diffusion.cpp Vulkan would work but needs model download/setup — "coming soon" in the UI, nothing in Core.

## Behaviour common to all providers
Timeouts 90 s (image) / 30 s (text); a user cancel surfaces as `OperationCanceledException`; every other failure (HTTP
status, network, HTML/empty/unreadable body, missing key) is an `AiException` with a short English message, scrubbed of
the key and of each `:`-separated part of it.
