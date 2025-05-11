cmake_minimum_required (VERSION 3.18)             # 3.18+ → reliable Android variables
project (PaperPlugin1 LANGUAGES CXX)

# ──────────────────────────────────────────────
# 0.  Global compiler settings
# ──────────────────────────────────────────────
set (CMAKE_CXX_STANDARD 17)
set (CMAKE_CXX_STANDARD_REQUIRED ON)
set (CMAKE_POSITION_INDEPENDENT_CODE ON)          # safety for shared libs

# ──────────────────────────────────────────────
# 1.  Source files – keep pch / dllmain out of Android
# ──────────────────────────────────────────────
file (GLOB_RECURSE SRC_COMMON CONFIGURE_DEPENDS
      "${CMAKE_CURRENT_SOURCE_DIR}/PaperPlugin1/*.cpp")

if (ANDROID)
    list (FILTER SRC_COMMON EXCLUDE REGEX
          "PaperPlugin1[/\\\\](dllmain|pch)\\.cpp$")    # CMake ≥3.6 :contentReference[oaicite:0]{index=0}
endif()

add_library (PaperPlugin1 SHARED ${SRC_COMMON})
target_include_directories (PaperPlugin1 PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/PaperPlugin1)

# ──────────────────────────────────────────────
# 2.  Android-specific section
# ──────────────────────────────────────────────
if (ANDROID)
    # CMake ≥3.7 guarantees CMAKE_ANDROID_ARCH_ABI :contentReference[oaicite:1]{index=1}
    if (NOT DEFINED CMAKE_ANDROID_ARCH_ABI)
        set (CMAKE_ANDROID_ARCH_ABI "${ANDROID_ABI}")  # fallback for very old CMake
    endif()

    # —— locate OpenCV shared object that you copied to jniLibs/ABI/
    get_filename_component (REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../.." ABSOLUTE)
    set (OPENCV_SO
         "${REPO_ROOT}/AndroidPlugins/paperplugin/src/main/jniLibs/${CMAKE_ANDROID_ARCH_ABI}/libopencv_java4.so")

    if (NOT EXISTS "${OPENCV_SO}")
        message (FATAL_ERROR "libopencv_java4.so NOT found at: ${OPENCV_SO}")
    endif()

    add_library (opencv_java4 SHARED IMPORTED GLOBAL)
    set_target_properties (opencv_java4 PROPERTIES IMPORTED_LOCATION "${OPENCV_SO}")

    # —— system libs
    find_library (log-lib      log)
    find_library (android-lib  android)

    target_link_libraries (PaperPlugin1
        PRIVATE
            opencv_java4
            ${log-lib}
            ${android-lib})
else()
# ──────────────────────────────────────────────
# 3.  Desktop / Windows section
#    (nothing Android-specific leaks in here)
# ──────────────────────────────────────────────
    target_compile_definitions (PaperPlugin1 PRIVATE _CRT_SECURE_NO_WARNINGS)
    # add Windows-only sources or libraries here if you need them
endif()
