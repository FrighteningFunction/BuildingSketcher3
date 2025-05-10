// PaperPlugin.cpp

#include "pch.h"

#include "PaperPlugin.h"
#include <opencv2/opencv.hpp>



bool FindPaperCorners(unsigned char* imageData, int width, int height, float* outCorners) {
    cv::Mat img(height, width, CV_8UC4, imageData); // ARFoundation usually gives RGBA

    cv::Mat gray;
    cv::cvtColor(img, gray, cv::COLOR_RGBA2GRAY);
    cv::GaussianBlur(gray, gray, cv::Size(5, 5), 0);

    std::vector<std::vector<cv::Point>> contours;
    cv::findContours(gray, contours, cv::RETR_EXTERNAL, cv::CHAIN_APPROX_SIMPLE);

    for (const auto& c : contours) {
        double peri = cv::arcLength(c, true);
        std::vector<cv::Point> approx;
        cv::approxPolyDP(c, approx, 0.02 * peri, true);

        if (approx.size() == 4 && cv::isContourConvex(approx)) {
            for (int i = 0; i < 4; i++) {
                outCorners[i * 2 + 0] = static_cast<float>(approx[i].x);
                outCorners[i * 2 + 1] = static_cast<float>(approx[i].y);
            }
            return true;
        }
    }

    return false;
}

