﻿// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// Microsoft Public License (MS-PL)
// 
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
// 
// 1. Definitions
// 
//   The terms "reproduce," "reproduction," "derivative works," and
//   "distribution" have the same meaning here as under U.S. copyright law. A
//   "contribution" is the original software, or any additions or changes to
//   the software. A "contributor" is any person that distributes its
//   contribution under this license. "Licensed patents" are a contributor's
//   patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
//   (A) Copyright Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free copyright license
//       to reproduce its contribution, prepare derivative works of its
//       contribution, and distribute its contribution or any derivative works
//       that you create.
// 
//   (B) Patent Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free license under its
//       licensed patents to make, have made, use, sell, offer for sale,
//       import, and/or otherwise dispose of its contribution in the software
//       or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
//   (A) No Trademark License- This license does not grant you rights to use
//       any contributors' name, logo, or trademarks.
// 
//   (B) If you bring a patent claim against any contributor over patents that
//       you claim are infringed by the software, your patent license from such
//       contributor to the software ends automatically.
// 
//   (C) If you distribute any portion of the software, you must retain all
//       copyright, patent, trademark, and attribution notices that are present
//       in the software.
// 
//   (D) If you distribute any portion of the software in source code form, you
//       may do so only under this license by including a complete copy of this
//       license with your distribution. If you distribute any portion of the
//       software in compiled or object code form, you may only do so under a
//       license that complies with this license.
// 
//   (E) The software is licensed "as-is." You bear the risk of using it. The
//       contributors give no express warranties, guarantees or conditions. You
//       may have additional consumer rights under your local laws which this
//       license cannot change. To the extent permitted under your local laws,
//       the contributors exclude the implied warranties of merchantability,
//       fitness for a particular purpose and non-infringement.
//       

#pragma once

//-----------------------------------------------------------------------------
// V8Context
//-----------------------------------------------------------------------------

class V8Context: public WeakRefTarget<V8Context>
{
public:

    static V8Context* Create(const SharedPtr<V8Isolate>& spIsolate, const StdString& name, bool enableDebugging, bool disableGlobalMembers, int debugPort);
    static size_t GetInstanceCount();

    virtual size_t GetMaxIsolateHeapSize() = 0;
    virtual void SetMaxIsolateHeapSize(size_t value) = 0;
    virtual double GetIsolateHeapSizeSampleInterval() = 0;
    virtual void SetIsolateHeapSizeSampleInterval(double value) = 0;

    virtual size_t GetMaxIsolateStackUsage() = 0;
    virtual void SetMaxIsolateStackUsage(size_t value) = 0;

    typedef void LockCallbackT(void* pvArg);
    virtual void CallWithLock(LockCallbackT* pCallback, void* pvArg) = 0;

    virtual V8Value GetRootObject() = 0;
    virtual void SetGlobalProperty(const StdString& name, const V8Value& value, bool globalMembers) = 0;
    virtual V8Value Execute(const StdString& documentName, const StdString& code, bool evaluate, bool discard) = 0;

    virtual V8ScriptHolder* Compile(const StdString& documentName, const StdString& code) = 0;
    virtual bool CanExecute(V8ScriptHolder* pHolder) = 0;
    virtual V8Value Execute(V8ScriptHolder* pHolder, bool evaluate) = 0;

    virtual void Interrupt() = 0;
    virtual void GetIsolateHeapInfo(V8IsolateHeapInfo& heapInfo) = 0;
    virtual void CollectGarbage(bool exhaustive) = 0;
	virtual void WriteHeapSnapshot(const  char* fileName) = 0;
    virtual void OnAccessSettingsChanged() = 0;

    virtual void Destroy() = 0;

protected:

    virtual ~V8Context() {};
};

//-----------------------------------------------------------------------------
// SharedPtrTraits<V8Context>
//-----------------------------------------------------------------------------

template<>
class SharedPtrTraits<V8Context>
{
    PROHIBIT_CONSTRUCT(SharedPtrTraits)

public:

    static void Destroy(V8Context* pTarget)
    {
        pTarget->Destroy();
    }
};
