#!/bin/bash
set -e
echo "=== Building FMOD stub libraries for armeabi-v7a ==="

# We need NDK to cross-compile for ARM. Docker image has Android SDK.
# Find NDK
NDK_DIR=$(find /opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer -name "ndk-bundle" -o -name "ndk" -type d 2>/dev/null | head -1)
if [ -z "$NDK_DIR" ]; then
  NDK_DIR=$(find /usr/local -name "ndk*" -type d 2>/dev/null | head -1)
fi
if [ -z "$NDK_DIR" ]; then
  # Try Android SDK location
  NDK_DIR=$(find / -path "*/ndk/*/toolchains" -type d 2>/dev/null | sed 's|/toolchains||' | head -1)
fi

echo "NDK search result: $NDK_DIR"

# Find ARM GCC or clang in the NDK
ARM_CC=""
if [ -n "$NDK_DIR" ]; then
  ARM_CC=$(find "$NDK_DIR" -name "arm-linux-androideabi-gcc" -o -name "armv7a-linux-androideabi*-clang" 2>/dev/null | grep -v "clang++" | head -1)
fi

# Alternative: try the standalone toolchain
if [ -z "$ARM_CC" ]; then
  ARM_CC=$(find / -name "arm-linux-androideabi-gcc" 2>/dev/null | head -1)
fi
if [ -z "$ARM_CC" ]; then
  ARM_CC=$(find / -name "armv7a-linux-androideabi21-clang" 2>/dev/null | head -1)
fi
if [ -z "$ARM_CC" ]; then
  ARM_CC=$(find / -path "*armv7a*clang" ! -name "*++" 2>/dev/null | head -1)
fi

if [ -z "$ARM_CC" ]; then
  echo "ERROR: No ARM cross-compiler found. Attempting apt install..."
  apt-get update -qq && apt-get install -y -qq gcc-arm-linux-gnueabihf
  ARM_CC="arm-linux-gnueabihf-gcc"
fi

echo "Using compiler: $ARM_CC"

# Create minimal C stub that exports FMOD functions the game calls
cat > /tmp/fmod_stub.c << 'STUBEOF'
/* Minimal FMOD stub - exports symbols to prevent DllNotFoundException */
/* These are no-op implementations that return success codes */

typedef int FMOD_RESULT;
#define FMOD_OK 0

/* Core FMOD functions */
FMOD_RESULT FMOD_System_Create(void** system) { if(system) *system = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_System_Init(void* system, int maxchannels, int flags, void* extra) { return FMOD_OK; }
FMOD_RESULT FMOD_System_Close(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_System_Release(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_System_Update(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_System_CreateSound(void* system, const char* name, int mode, void* info, void** sound) { if(sound) *sound = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_System_PlaySound(void* system, void* sound, void* group, int paused, void** channel) { if(channel) *channel = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_System_GetVersion(void* system, unsigned int* version) { if(version) *version = 0x00020109; return FMOD_OK; }
FMOD_RESULT FMOD_System_SetOutput(void* system, int output) { return FMOD_OK; }
FMOD_RESULT FMOD_System_SetSoftwareFormat(void* system, int samplerate, int speakermode, int numrawspeakers) { return FMOD_OK; }
FMOD_RESULT FMOD_System_GetMasterChannelGroup(void* system, void** group) { if(group) *group = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Sound_Release(void* sound) { return FMOD_OK; }
FMOD_RESULT FMOD_Channel_Stop(void* channel) { return FMOD_OK; }
FMOD_RESULT FMOD_Channel_SetVolume(void* channel, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD_Channel_IsPlaying(void* channel, int* playing) { if(playing) *playing = 0; return FMOD_OK; }
FMOD_RESULT FMOD_ChannelGroup_SetVolume(void* group, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD_Memory_Initialize(void* pool, int poollen, void* alloc, void* realloc, void* free, int type) { return FMOD_OK; }
FMOD_RESULT FMOD_Debug_Initialize(int flags, int mode, void* callback, const char* filename) { return FMOD_OK; }
STUBEOF

cat > /tmp/fmodstudio_stub.c << 'STUBEOF'
/* Minimal FMOD Studio stub */
typedef int FMOD_RESULT;
#define FMOD_OK 0

/* Studio system functions */
FMOD_RESULT FMOD_Studio_System_Create(void** system, unsigned int headerversion) { if(system) *system = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_Initialize(void* system, int maxchannels, int studioflags, int flags, void* extra) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_Release(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_Update(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_GetCoreSystem(void* system, void** coresystem) { if(coresystem) *coresystem = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_LoadBankFile(void* system, const char* filename, int flags, void** bank) { if(bank) *bank = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_LoadBankMemory(void* system, const char* buffer, int length, int mode, int flags, void** bank) { if(bank) *bank = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_GetEvent(void* system, const char* path, void** event) { if(event) *event = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_GetBus(void* system, const char* path, void** bus) { if(bus) *bus = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_GetVCA(void* system, const char* path, void** vca) { if(vca) *vca = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_LookupID(void* system, const char* path, void* id) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_FlushCommands(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_FlushSampleLoading(void* system) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_System_SetListenerAttributes(void* system, int index, void* attrs, void* attvec) { return FMOD_OK; }

/* Event */
FMOD_RESULT FMOD_Studio_EventDescription_CreateInstance(void* desc, void** instance) { if(instance) *instance = (void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventDescription_GetInstanceCount(void* desc, int* count) { if(count) *count = 0; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventDescription_IsOneshot(void* desc, int* oneshot) { if(oneshot) *oneshot = 1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventDescription_Is3D(void* desc, int* is3d) { if(is3d) *is3d = 0; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_Start(void* instance) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_Stop(void* instance, int mode) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_Release(void* instance) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_SetParameterByName(void* instance, const char* name, float value, int ignoreseek) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_GetParameterByName(void* instance, const char* name, float* value, float* finalvalue) { if(value) *value = 0; if(finalvalue) *finalvalue = 0; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_Set3DAttributes(void* instance, void* attrs) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_GetPlaybackState(void* instance, int* state) { if(state) *state = 2; return FMOD_OK; } /* STOPPED=2 */
FMOD_RESULT FMOD_Studio_EventInstance_SetVolume(void* instance, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_EventInstance_GetDescription(void* instance, void** desc) { if(desc) *desc = (void*)0x1; return FMOD_OK; }

/* Bank */
FMOD_RESULT FMOD_Studio_Bank_Unload(void* bank) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_Bank_GetEventCount(void* bank, int* count) { if(count) *count = 0; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_Bank_LoadSampleData(void* bank) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_Bank_GetLoadingState(void* bank, int* state) { if(state) *state = 2; return FMOD_OK; } /* LOADED=2 */

/* Bus */
FMOD_RESULT FMOD_Studio_Bus_SetVolume(void* bus, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_Bus_GetVolume(void* bus, float* volume, float* finalvol) { if(volume) *volume=1; if(finalvol) *finalvol=1; return FMOD_OK; }
FMOD_RESULT FMOD_Studio_Bus_SetPaused(void* bus, int paused) { return FMOD_OK; }

/* VCA */
FMOD_RESULT FMOD_Studio_VCA_SetVolume(void* vca, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD_Studio_VCA_GetVolume(void* vca, float* volume, float* finalvol) { if(volume) *volume=1; if(finalvol) *finalvol=1; return FMOD_OK; }
STUBEOF

# Compile for armeabi-v7a
mkdir -p /tmp/fmod-stubs
$ARM_CC -shared -o /tmp/fmod-stubs/libfmod.so /tmp/fmod_stub.c -Wl,-soname,libfmod.so 2>/dev/null || \
$ARM_CC -shared -o /tmp/fmod-stubs/libfmod.so /tmp/fmod_stub.c -nostdlib 2>/dev/null || \
$ARM_CC -shared -o /tmp/fmod-stubs/libfmod.so /tmp/fmod_stub.c

$ARM_CC -shared -o /tmp/fmod-stubs/libfmodstudio.so /tmp/fmodstudio_stub.c -Wl,-soname,libfmodstudio.so 2>/dev/null || \
$ARM_CC -shared -o /tmp/fmod-stubs/libfmodstudio.so /tmp/fmodstudio_stub.c -nostdlib 2>/dev/null || \
$ARM_CC -shared -o /tmp/fmod-stubs/libfmodstudio.so /tmp/fmodstudio_stub.c

echo "=== Built FMOD stubs ==="
ls -la /tmp/fmod-stubs/
file /tmp/fmod-stubs/libfmod.so
file /tmp/fmod-stubs/libfmodstudio.so

# Place in project
PLUGIN_DIR="/project/Assets/Plugins/Android/libs/armeabi-v7a"
mkdir -p "$PLUGIN_DIR"
cp /tmp/fmod-stubs/libfmod.so "$PLUGIN_DIR/"
cp /tmp/fmod-stubs/libfmodstudio.so "$PLUGIN_DIR/"

echo "=== Installed to $PLUGIN_DIR ==="
ls -la "$PLUGIN_DIR/"
