// LineDetector.cpp

#include "pch.h"
#include <opencv2/opencv.hpp>
#include <vector>

// ---------------------------------------------------------------------------
// Merge HoughLinesP segments that are nearly parallel *and* close together.
// Each output line is (x1,y1,x2,y2).
// angleTolDeg  – max angle difference for grouping   (e.g. 5–7°)
// distTolPx    – max distance between two lines to be considered the same
// ---------------------------------------------------------------------------
static std::vector<cv::Vec4i>
MergeColinearClusters(const std::vector<cv::Vec4i>& segs,
    double angleTolDeg = 7.0,
    double distTolPx = 15.0)
{
    struct Cluster {
        cv::Vec4i ref;                 // representative segment
        std::vector<cv::Point2f> pts;  // all endpoints
    };
    std::vector<Cluster> clusters;

    auto angleOf = [](const cv::Vec4i& v)
        {
            return std::atan2(double(v[3] - v[1]), double(v[2] - v[0])) * 180.0 / CV_PI;
        };
    auto distPtLine = [](cv::Point2f p, const cv::Vec4i& l)
        {
            cv::Point2f a(l[0], l[1]), b(l[2], l[3]);
            cv::Point2f ab = b - a, ap = p - a;
            double area = std::abs(ab.x * ap.y - ab.y * ap.x);
            double len = std::hypot(ab.x, ab.y);
            return len == 0 ? 1e9 : area / len;              // distance in px
        };

    for (const auto& s : segs)
    {
        double ang = angleOf(s);
        bool placed = false;

        for (auto& c : clusters)
        {
            double ang2 = angleOf(c.ref);
            if (std::abs(ang - ang2) > angleTolDeg &&
                std::abs(ang - ang2 + 180) > angleTolDeg &&
                std::abs(ang - ang2 - 180) > angleTolDeg)
                continue;                             // orientation too different

            // test distance of both endpoints to representative line
            cv::Point2f p1(s[0], s[1]), p2(s[2], s[3]);
            if (distPtLine(p1, c.ref) < distTolPx &&
                distPtLine(p2, c.ref) < distTolPx)
            {
                c.pts.push_back(p1);
                c.pts.push_back(p2);
                placed = true;
                break;
            }
        }
        if (!placed)
        {
            Cluster c;
            c.ref = s;
            c.pts.push_back({ float(s[0]), float(s[1]) });
            c.pts.push_back({ float(s[2]), float(s[3]) });
            clusters.push_back(c);
        }
    }

    // For each cluster build one spanning segment
    std::vector<cv::Vec4i> merged;
    merged.reserve(clusters.size());

    for (const auto& c : clusters)
    {
        // use principal axis: just take min/max projection along cluster's ref
        cv::Vec4i r = c.ref;
        cv::Point2f a(r[0], r[1]), b(r[2], r[3]);
        cv::Point2f dir = b - a;
        double len = std::hypot(dir.x, dir.y);
        if (len == 0) continue;
        dir.x /= len; dir.y /= len;

        double tmin = 1e9, tmax = -1e9;
        for (auto p : c.pts)
        {
            double t = (p.x - a.x) * dir.x + (p.y - a.y) * dir.y;
            tmin = std::min(tmin, t);
            tmax = std::max(tmax, t);
        }
        cv::Point2f p1 = a + tmin * dir;
        cv::Point2f p2 = a + tmax * dir;
        merged.emplace_back(cvRound(p1.x), cvRound(p1.y),
            cvRound(p2.x), cvRound(p2.y));
    }
    return merged;
}

extern "C" __declspec(dllexport)
int FindBlackLines(unsigned char* imageData, int width, int height, float* outLines, int maxLines)
{
    cv::Mat img(height, width, CV_8UC4, imageData);
    cv::Mat gray;
    cv::cvtColor(img, gray, cv::COLOR_BGR2GRAY);


    cv::Mat blurred;
    cv::GaussianBlur(gray, blurred, cv::Size(9, 9), 0);


    cv::Mat bw;
    cv::adaptiveThreshold(blurred, bw, 255, cv::ADAPTIVE_THRESH_MEAN_C, cv::THRESH_BINARY_INV, 21, 10);

    // Use Canny edge detector to emphasize edges
    cv::Mat edges;
    cv::Canny(bw, edges, 50, 200, 3);

    cv::Mat closed;
    cv::morphologyEx(edges, closed, cv::MORPH_CLOSE, cv::Mat(), cv::Point(-1, -1), 2);

    // Use HoughLinesP to find straight lines
    std::vector<cv::Vec4i> lines;

	int minLineLength = 250;
	int maxLineGap = 50;
    int houghThreshold = 90;

    cv::HoughLinesP(closed, lines, 1, CV_PI / 180, houghThreshold, minLineLength, maxLineGap);

    std::vector<cv::Vec4i> merged = MergeColinearClusters(lines);

    int toExport = std::min<int>(merged.size(), maxLines);
    for (int i = 0; i < toExport; ++i)
    {
        const cv::Vec4i& l = merged[i];
        outLines[i * 4 + 0] = static_cast<float>(l[0]);
        outLines[i * 4 + 1] = static_cast<float>(l[1]);
        outLines[i * 4 + 2] = static_cast<float>(l[2]);
        outLines[i * 4 + 3] = static_cast<float>(l[3]);
    }
    return toExport;
}
