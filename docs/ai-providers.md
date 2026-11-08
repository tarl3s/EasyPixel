# AI providers (Create with AI)

Code: `src/EasyPixel.Core/Ai/Providers.cs` (interfaces, `AiProviders` registry) and `src/EasyPixel.Core/Ai/Providers/`.
Checked on **2026-11-08**. Providers return raw image bytes; decoding (PNG, and JPEG/WebP through Avalonia's `Bitmap`)
happens in the app layer (`ImageDecode`, Task 5). Every provider asks for PNG where the API allows it.

Status legend: **verified** = confirmed against the provider's docs or a reliable report today; **from memory** = the
shape EasyPixel was written against, not re-confirmed today (web search could not reach the official reference). Check
these first if a provider starts failing.

## Image generators (order in `AiProviders.Images`: free first)

| Id | Free | Request | Response | Status |
|---|---|---|---|---|
| `cloudflare` | yes (account) | `POST https://api.cloudflare.com/client/v4/accounts/{account}/ai/run/@cf/black-forest-labs/flux-1-schnell`, `Authorization: Bearer {token}`, `{prompt, steps:4, seed}` | `{"result":{"image":"<base64>"}}` (JPEG) | route + free tier verified (controller research); response shape from memory |
| `pollinations` | yes (key) | `GET https://gen.pollinations.ai/image/{prompt}?model=flux&width=512&height=512&seed=&nologo=true`, `Authorization: Bearer sk_…` | image bytes | from memory; free flux on the starter tier unverified |
| `openai` | paid | `POST https://api.openai.com/v1/images/generations`, Bearer, `{model:"gpt-image-1", prompt, size:"1024x1024", quality:"low", output_format:"png"}` | `data[0].b64_json` | from memory; no seed in the API (ignored) |
| `gemini` | paid | `POST https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent`, header `x-goog-api-key`, `generationConfig.responseModalities:["IMAGE"]` | `candidates[0].content.parts[].inlineData.data` | no free tier for image models (controller research, Aug 2026); shape from memory |
| `retrodiffusion` | paid | `POST https://api.retrodiffusion.ai/v1/inferences`, header `X-RD-Token`, `{prompt, width:128, height:128, num_images:1, seed, prompt_style:"rd_fast__default"}` | `{"base64_images":["<png>"]}` | pricing/limits from controller research; request shape from memory — **official reference not reached today**, see retrodiffusion.ai/app/guide/api and github.com/Retro-Diffusion/api-examples |

### Cloudflare key format
EasyPixel stores Cloudflare as ONE key string: `accountId:apiToken`. The Account ID is on the Workers AI dashboard;
the token needs the "Workers AI" permission. A key without `:` is refused with a friendly message before any request.

## Text AIs (translation to English)

| Id | Free | Request | Response |
|---|---|---|---|
| `cloudflare` | yes | same route, model `@cf/meta/llama-3.1-8b-instruct`, `{messages:[system,user]}` | `result.response` |
| `gemini` | free tier with key | `gemini-2.5-flash:generateContent`, `systemInstruction` + `contents` | `candidates[0].content.parts[].text` |
| `openai` | paid | `POST /v1/chat/completions`, `gpt-4o-mini` | `choices[0].message.content` |

Model ids are from memory; verify the Cloudflare Llama id and the OpenAI model if they return 404/400.

## Sign-up and limits

- Cloudflare Workers AI: https://dash.cloudflare.com/sign-up — 10,000 neurons/day free (reset 00:00 UTC); flux-1-schnell ≈ 43 neurons per 512² 4-step image (~200/day).
- Pollinations: https://enter.pollinations.ai — `sk_` secret keys.
- OpenAI: https://platform.openai.com/api-keys — paid per image.
- Google AI Studio: https://aistudio.google.com/apikey — text free tier; image ≈ $0.039/image.
- Retro Diffusion: https://retrodiffusion.ai — ≈ $0.01–0.18/image, 10 requests/min/key, sizes 16–384 by style.

## Not shipped
- Keyless generators: legacy `image.pollinations.ai` watermarks then returns 402; `gen.pollinations.ai` needs a key.
- Hugging Face: free monthly credits removed (Oct 2026).
- Local (Windows + AMD): stable-diffusion.cpp Vulkan would work but needs model download/setup — "coming soon" in the UI, nothing in Core.

## Behaviour common to all providers
Timeouts 90 s (image) / 30 s (text) per request; a user cancel surfaces as `OperationCanceledException`; every other
failure (HTTP status, network, HTML/empty/unreadable body, missing key) is an `AiException` with a short English
message, scrubbed of the key and of each `:`-separated part of it.
