//
// pch.h
// Header for standard system include files.
//

#pragma once

#include <collection.h>
#include <ppltasks.h>

template <class T> void SafeRelease(__deref_inout_opt T **ppT)
{
	T *pTTemp = *ppT;    // temp copy
	*ppT = nullptr;      // zero the input
	if (pTTemp)
	{
		pTTemp->Release();
	}
}

#ifndef SAFE_RELEASE
#define SAFE_RELEASE(x) { SafeRelease(&x); }
#endif