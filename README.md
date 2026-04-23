# MauiLLM

Android-first .NET MAUI demo for running `Phi-3-mini-4k-instruct-onnx` entirely on-device with `Microsoft.ML.OnnxRuntimeGenAI` `0.13.1`.

Android builds with this package require `minSdkVersion 24`.

## Model variant

Use the mobile-optimized Phi-3 files from:

- `cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4`

This is the recommended CPU/mobile variant for phones. The app expects the contents of that folder to be present on-device before launch.

## Android setup

1. Build and install the app on a physical Android device.
2. The app checks these folders in order:
   - `/sdcard/Download/phi3-mini`
   - `/sdcard/Download/cpu-int4-rtn-block-32-acc-level-4`
   - `/sdcard/phi3-mini`
   - `/sdcard/Android/data/com.companyname.mauillm/files/models/phi3-mini`
   - `/sdcard/Android/data/com.companyname.mauillm/files/models/cpu-int4-rtn-block-32-acc-level-4`
3. Start the app and wait for the model to load.

Recommended `adb` flow:

```bash
adb shell mkdir -p /sdcard/Download/phi3-mini
adb push cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4/. /sdcard/Download/phi3-mini/
```

## Notes for the article

- The correct package reference is:

```xml
<PackageReference Include="Microsoft.ML.OnnxRuntimeGenAI" Version="0.13.1" />
```

- The original "download manager" idea is intentionally out of scope for this demo. This project assumes the model is already copied to the device so the article can stay focused on local inference, prompt formatting, and UI streaming.
