#!/bin/bash
set -e
echo "=== Building FMOD5_ stub libraries for armeabi-v7a (Bionic ABI) ==="

# We need NDK to cross-compile for ARM Bionic (NOT glibc!).
# A .so built with arm-linux-gnueabihf-gcc targets glibc hard-float and will
# fail to load on Android (UnsatisfiedLinkError / wrong ELF interpreter).
NDK_DIR=$(find /opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer -name "ndk-bundle" -o -name "ndk" -type d 2>/dev/null | head -1)
if [ -z "$NDK_DIR" ]; then
  NDK_DIR=$(find /usr/local -name "ndk*" -type d 2>/dev/null | head -1)
fi
if [ -z "$NDK_DIR" ]; then
  NDK_DIR=$(find / -path "*/ndk/*/toolchains" -type d 2>/dev/null | sed 's|/toolchains||' | head -1)
fi

echo "NDK search result: $NDK_DIR"

# Resolve the NDK clang for armeabi-v7a with a reasonable API level
ARM_CC=""
if [ -n "$NDK_DIR" ]; then
  ARM_CC=$(find "$NDK_DIR" -name "armv7a-linux-androideabi*-clang" 2>/dev/null | grep -v "clang++" | sort -r | head -1)
fi
# Fallback: search entire filesystem
if [ -z "$ARM_CC" ]; then
  ARM_CC=$(find / -name "armv7a-linux-androideabi*-clang" 2>/dev/null | grep -v "clang++" | sort -r | head -1)
fi
if [ -z "$ARM_CC" ]; then
  ARM_CC=$(find / -name "arm-linux-androideabi-gcc" 2>/dev/null | head -1)
fi

if [ -z "$ARM_CC" ]; then
  echo "FATAL: No NDK ARM cross-compiler found. Cannot build Bionic-compatible .so files."
  echo "Install the Android NDK (r21+) and set ANDROID_NDK_HOME."
  exit 1
fi

echo "Using compiler: $ARM_CC"
"$ARM_CC" --version 2>&1 | head -1 || true

# The Unity FMOD C# bindings (fmod.dll / fmodstudio.dll) call FMOD5_-prefixed
# C functions. The EntryPointNotFoundException in the crash log is because the
# old stubs exported FMOD_System_Create etc. instead of FMOD5_System_Create.

cat > /tmp/fmod5_stub.c << 'STUBEOF'
typedef int FMOD_RESULT;
typedef unsigned int FMOD_BOOL;
#define FMOD_OK 0

/* === FMOD5 Core — what the Unity C# binding calls === */
FMOD_RESULT FMOD5_Memory_GetStats(int* currentalloced, int* maxalloced, FMOD_BOOL blocking) {
  if(currentalloced) *currentalloced = 0;
  if(maxalloced) *maxalloced = 0;
  return FMOD_OK;
}
FMOD_RESULT FMOD5_Memory_Initialize(void* pool, int poollen, void* alloc, void* realloc, void* free, int type) { return FMOD_OK; }
FMOD_RESULT FMOD5_Debug_Initialize(int flags, int mode, void* callback, const char* filename) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_Create(void** system, unsigned int headerversion) { if(system) *system=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_System_Init(void* sys, int maxchannels, int flags, void* extra) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_Close(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_Release(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_Update(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_SetOutput(void* sys, int output) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_GetVersion(void* sys, unsigned int* version) { if(version) *version=0x00020109; return FMOD_OK; }
FMOD_RESULT FMOD5_System_GetNumDrivers(void* sys, int* numdrivers) { if(numdrivers) *numdrivers=1; return FMOD_OK; }
FMOD_RESULT FMOD5_System_GetDriverInfo(void* sys, int id, char* name, int namelen, void* guid, int* rate, int* mode, int* channels) { if(rate)*rate=48000; if(channels)*channels=2; return FMOD_OK; }
FMOD_RESULT FMOD5_System_SetSoftwareFormat(void* sys, int rate, int mode, int numraw) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_SetSoftwareChannels(void* sys, int numsoftwarechannels) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_SetDSPBufferSize(void* sys, unsigned int size, int count) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_SetAdvancedSettings(void* sys, void* settings) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_GetMasterChannelGroup(void* sys, void** group) { if(group) *group=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_System_CreateSound(void* sys, const char* url, int mode, void* info, void** sound) { if(sound) *sound=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_System_PlaySound(void* sys, void* sound, void* grp, FMOD_BOOL paused, void** channel) { if(channel) *channel=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_System_MixerSuspend(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_MixerResume(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_AttachChannelGroupToPort(void* sys, int portType, long long portIndex, void* cg, FMOD_BOOL passThru) { return FMOD_OK; }
FMOD_RESULT FMOD5_System_DetachChannelGroupFromPort(void* sys, void* cg) { return FMOD_OK; }
FMOD_RESULT FMOD5_Sound_Release(void* sound) { return FMOD_OK; }
FMOD_RESULT FMOD5_Sound_GetLength(void* sound, unsigned int* length, int type) { if(length) *length=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Channel_Stop(void* ch) { return FMOD_OK; }
FMOD_RESULT FMOD5_Channel_SetVolume(void* ch, float vol) { return FMOD_OK; }
FMOD_RESULT FMOD5_Channel_GetPosition(void* ch, unsigned int* pos, int unit) { if(pos) *pos=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Channel_IsPlaying(void* ch, FMOD_BOOL* playing) { if(playing) *playing=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Channel_SetPaused(void* ch, FMOD_BOOL paused) { return FMOD_OK; }
FMOD_RESULT FMOD5_ChannelGroup_SetVolume(void* cg, float vol) { return FMOD_OK; }
FMOD_RESULT FMOD5_ChannelGroup_GetVolume(void* cg, float* vol) { if(vol) *vol=1.0f; return FMOD_OK; }
FMOD_RESULT FMOD5_ChannelGroup_Stop(void* cg) { return FMOD_OK; }
STUBEOF

cat > /tmp/fmod5studio_stub.c << 'STUBEOF'
typedef int FMOD_RESULT;
typedef unsigned int FMOD_BOOL;
#define FMOD_OK 0

/* === FMOD5 Studio — what the Unity C# binding calls === */
FMOD_RESULT FMOD5_Studio_System_Create(void** system, unsigned int headerversion) { if(system) *system=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_IsValid(void* sys, FMOD_BOOL* valid) { if(valid) *valid=1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_Initialize(void* sys, int maxch, int studioflags, int flags, void* extra) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_Release(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_Update(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetCoreSystem(void* sys, void** core) { if(core) *core=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_FlushCommands(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_FlushSampleLoading(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_SetListenerAttributes(void* sys, int idx, void* attr, void* atten) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetListenerAttributes(void* sys, int idx, void* attr, void* atten) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_LoadBankFile(void* sys, const char* f, int flags, void** bank) { if(bank) *bank=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_LoadBankMemory(void* sys, const char* buf, int len, int mode, int flags, void** bank) { if(bank) *bank=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_LoadBankCustom(void* sys, void* info, int flags, void** bank) { if(bank) *bank=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_UnloadAll(void* sys) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetEvent(void* sys, const char* path, void** event) { if(event) *event=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetBus(void* sys, const char* path, void** bus) { if(bus) *bus=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetVCA(void* sys, const char* path, void** vca) { if(vca) *vca=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_GetBank(void* sys, const char* path, void** bank) { if(bank) *bank=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_LookupID(void* sys, const char* path, void* id) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_System_LookupPath(void* sys, void* id, char* path, int size, int* retrieved) { if(retrieved) *retrieved=0; return FMOD_OK; }

/* Event Description */
FMOD_RESULT FMOD5_Studio_EventDescription_CreateInstance(void* desc, void** instance) { if(instance) *instance=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_GetInstanceCount(void* desc, int* count) { if(count) *count=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_GetInstanceList(void* desc, void** arr, int cap, int* count) { if(count) *count=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_IsOneshot(void* desc, FMOD_BOOL* oneshot) { if(oneshot) *oneshot=1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_Is3D(void* desc, FMOD_BOOL* is3d) { if(is3d) *is3d=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_LoadSampleData(void* desc) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_GetSampleLoadingState(void* desc, int* state) { if(state) *state=2; return FMOD_OK; } /* LOADED=2 */
FMOD_RESULT FMOD5_Studio_EventDescription_ReleaseAllInstances(void* desc) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventDescription_GetPath(void* desc, char* path, int size, int* retrieved) { if(retrieved) *retrieved=0; return FMOD_OK; }

/* Event Instance */
FMOD_RESULT FMOD5_Studio_EventInstance_Start(void* instance) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_Stop(void* instance, int mode) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_Release(void* instance) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_SetParameterByName(void* instance, const char* name, float value, FMOD_BOOL ignoreseek) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_SetParameterByID(void* instance, void* id, float value, FMOD_BOOL ignoreseek) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_GetParameterByName(void* instance, const char* name, float* value, float* finalvalue) { if(value) *value=0; if(finalvalue) *finalvalue=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_Set3DAttributes(void* instance, void* attrs) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_GetPlaybackState(void* instance, int* state) { if(state) *state=2; return FMOD_OK; } /* STOPPED=2 */
FMOD_RESULT FMOD5_Studio_EventInstance_SetVolume(void* instance, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_GetVolume(void* instance, float* volume, float* finalvol) { if(volume) *volume=1; if(finalvol) *finalvol=1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_SetPaused(void* instance, FMOD_BOOL paused) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_GetPaused(void* instance, FMOD_BOOL* paused) { if(paused) *paused=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_GetDescription(void* instance, void** desc) { if(desc) *desc=(void*)0x1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_EventInstance_IsValid(void* instance, FMOD_BOOL* valid) { if(valid) *valid=1; return FMOD_OK; }

/* Bank */
FMOD_RESULT FMOD5_Studio_Bank_Unload(void* bank) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bank_GetEventCount(void* bank, int* count) { if(count) *count=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bank_GetEventList(void* bank, void** arr, int cap, int* count) { if(count) *count=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bank_LoadSampleData(void* bank) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bank_GetLoadingState(void* bank, int* state) { if(state) *state=2; return FMOD_OK; } /* LOADED=2 */

/* Bus */
FMOD_RESULT FMOD5_Studio_Bus_SetVolume(void* bus, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bus_GetVolume(void* bus, float* volume, float* finalvol) { if(volume) *volume=1; if(finalvol) *finalvol=1; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bus_SetPaused(void* bus, FMOD_BOOL paused) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bus_GetPaused(void* bus, FMOD_BOOL* paused) { if(paused) *paused=0; return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_Bus_StopAllEvents(void* bus, int mode) { return FMOD_OK; }

/* VCA */
FMOD_RESULT FMOD5_Studio_VCA_SetVolume(void* vca, float volume) { return FMOD_OK; }
FMOD_RESULT FMOD5_Studio_VCA_GetVolume(void* vca, float* volume, float* finalvol) { if(volume) *volume=1; if(finalvol) *finalvol=1; return FMOD_OK; }
STUBEOF

# Compile for armeabi-v7a (Bionic ABI)
mkdir -p /tmp/fmod-stubs
"$ARM_CC" -shared -target armv7a-linux-androideabi -Wl,-soname,libfmod.so -o /tmp/fmod-stubs/libfmod.so /tmp/fmod5_stub.c || {
  echo "WARNING: -target flag failed, retrying without..."
  "$ARM_CC" -shared -Wl,-soname,libfmod.so -o /tmp/fmod-stubs/libfmod.so /tmp/fmod5_stub.c
}

"$ARM_CC" -shared -target armv7a-linux-androideabi -Wl,-soname,libfmodstudio.so -o /tmp/fmod-stubs/libfmodstudio.so /tmp/fmod5studio_stub.c || {
  echo "WARNING: -target flag failed, retrying without..."
  "$ARM_CC" -shared -Wl,-soname,libfmodstudio.so -o /tmp/fmod-stubs/libfmodstudio.so /tmp/fmod5studio_stub.c
}

echo "=== Built FMOD5_ stubs ==="
ls -la /tmp/fmod-stubs/
file /tmp/fmod-stubs/libfmod.so
file /tmp/fmod-stubs/libfmodstudio.so

# Verify the FMOD5_ symbols are actually exported
echo "=== Verifying FMOD5_ exports ==="
NM_TOOL=$(dirname "$ARM_CC")/llvm-nm 2>/dev/null || nm
"$NM_TOOL" -D /tmp/fmod-stubs/libfmod.so 2>/dev/null | grep "FMOD5_" | head -10
"$NM_TOOL" -D /tmp/fmod-stubs/libfmodstudio.so 2>/dev/null | grep "FMOD5_" | head -10

# Place in project
PLUGIN_DIR="/project/Assets/Plugins/Android/libs/armeabi-v7a"
mkdir -p "$PLUGIN_DIR"
cp /tmp/fmod-stubs/libfmod.so "$PLUGIN_DIR/"
cp /tmp/fmod-stubs/libfmodstudio.so "$PLUGIN_DIR/"

echo "=== Installed to $PLUGIN_DIR ==="
ls -la "$PLUGIN_DIR/"
