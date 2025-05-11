// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the PAPERPLUGIN1_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// PAPERPLUGIN1_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef PAPERPLUGIN1_EXPORTS
#define PAPERPLUGIN1_API __declspec(dllexport)
#else
#define PAPERPLUGIN1_API __declspec(dllimport)
#endif

// This class is exported from the dll
class PAPERPLUGIN1_API CPaperPlugin1 {
public:
	CPaperPlugin1(void);
	// TODO: add your methods here.
};

extern PAPERPLUGIN1_API int nPaperPlugin1;

PAPERPLUGIN1_API int fnPaperPlugin1(void);
