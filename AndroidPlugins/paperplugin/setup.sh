# adjust these paths to where you unzipped OpenCV-android-sdk
OPENCV_SDK=C:/Android/opencv-4.11.0-android-sdk/OpenCV-android-sdk/sdk/native/libs

cp $OPENCV_SDK/arm64-v8a/libopencv_java4.so    src/main/jniLibs/arm64-v8a/

cp $OPENCV_SDK/armeabi-v7a/libopencv_java4.so    src/main/jniLibs/armeabi-v7a/