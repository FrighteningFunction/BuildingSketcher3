// PaperPlugin.h
#pragma once

#ifdef __cplusplus
extern "C" {
#endif

	__declspec(dllexport) bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners); // 4 points x,y -> 8 floats

#ifdef __cplusplus
}
#endif
