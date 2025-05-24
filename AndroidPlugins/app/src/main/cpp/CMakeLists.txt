cmake_minimum_required(VERSION 3.4.1)

project(PaperPlugin)

add_library(PaperPlugin SHARED
        PaperPlugin1.cpp
        LineDetector.cpp)

# Link OpenCV
set(OpenCV_DIR "${CMAKE_SOURCE_DIR}/OpenCV-android-sdk/sdk/native/jni")  # Adjust this if needed

find_package(OpenCV REQUIRED)

include_directories(${OpenCV_INCLUDE_DIRS})

target_link_libraries(PaperPlugin
        ${OpenCV_LIBS}
        android
        log)
