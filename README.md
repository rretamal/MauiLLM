# MauiLLM

Android-first .NET MAUI demo for running `Phi-3-mini-4k-instruct-onnx` entirely on-device with `Microsoft.ML.OnnxRuntimeGenAI` `0.13.1`.

Android builds with this package require `minSdkVersion 24`.

## Model variant

Use the mobile-optimized Phi-3 files from:

- `cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4`

This is the recommended CPU/mobile variant for phones. The app can download the files on demand, or you can still copy them manually for demos.

## Android setup

1. Build and install the app on a physical Android device.
2. If the app does not find the model, tap `Download model`.
3. The model is downloaded to the app-specific external files folder:
   - `/sdcard/Android/data/com.companyname.mauillm/files/models/phi3-mini`
4. The largest file is `phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data`, roughly 2.72 GB, so use WiFi and keep the app open.

Manual copy is still supported. The app checks these folders, but Android scoped storage may hide files from `Download` even when Windows Explorer shows them:
   - `/sdcard/Download/phi3-mini`
   - `/sdcard/Download/cpu-int4-rtn-block-32-acc-level-4`
   - `/sdcard/phi3-mini`
   - `/sdcard/Android/data/com.companyname.mauillm/files/models/phi3-mini`
   - `/sdcard/Android/data/com.companyname.mauillm/files/models/cpu-int4-rtn-block-32-acc-level-4`
