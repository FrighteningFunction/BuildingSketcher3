cmake_minimum_required(VERSION 3.10)
project(PaperPlugin1)

add_library(PaperPlugin1 SHARED PaperPlugin.cpp)

# OpenCV headers – we copy/include them in step-3
target_include_directories(PaperPlugin1 PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/opencv/include)

# link against the OpenCV .so names we’ll ship
set(OPENCV_LIBS opencv_core opencv_imgproc)
find_library(log-lib log)     # Android log (optional)

target_link_libraries(PaperPlugin1 ${OPENCV_LIBS} ${log-lib})
