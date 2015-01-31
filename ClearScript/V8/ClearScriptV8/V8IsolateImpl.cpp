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

#include "ClearScriptV8Native.h"



//-----------------------------------------------------------------------------
// V8ArrayBufferAllocator
//-----------------------------------------------------------------------------

class V8ArrayBufferAllocator: public ArrayBuffer::Allocator
{
public:

    static void EnsureInstalled();

    void* Allocate(size_t size);
    void* AllocateUninitialized(size_t size);
    void Free(void* pvData, size_t size);

private:

    V8ArrayBufferAllocator();

    static V8ArrayBufferAllocator ms_Instance;
    static std::once_flag ms_InstallationFlag;
};

//-----------------------------------------------------------------------------

void V8ArrayBufferAllocator::EnsureInstalled()
{
    std::call_once(ms_InstallationFlag, []
    {
        V8::SetArrayBufferAllocator(&ms_Instance);
    });
}

//-----------------------------------------------------------------------------

void* V8ArrayBufferAllocator::Allocate(size_t size)
{
    return calloc(1, size);
}

//-----------------------------------------------------------------------------

void* V8ArrayBufferAllocator::AllocateUninitialized(size_t size)
{
    return malloc(size);
}

//-----------------------------------------------------------------------------

void V8ArrayBufferAllocator::Free(void* pvData, size_t /*size*/)
{
    free(pvData);
}

//-----------------------------------------------------------------------------

V8ArrayBufferAllocator::V8ArrayBufferAllocator()
{
}

//-----------------------------------------------------------------------------

V8ArrayBufferAllocator V8ArrayBufferAllocator::ms_Instance;
std::once_flag V8ArrayBufferAllocator::ms_InstallationFlag;

//-----------------------------------------------------------------------------
// V8IsolateImpl implementation
//-----------------------------------------------------------------------------

#define BEGIN_ISOLATE_ENTRY_SCOPE \
    { \
        Isolate::Scope t_IsolateEntryScope(m_pIsolate);

#define END_ISOLATE_ENTRY_SCOPE \
        IGNORE_UNUSED(t_IsolateEntryScope); \
    }

#define BEGIN_ISOLATE_SCOPE \
    { \
        Scope t_IsolateScope(this);

#define END_ISOLATE_SCOPE \
        IGNORE_UNUSED(t_IsolateScope); \
    }

//-----------------------------------------------------------------------------

DEFINE_CONCURRENT_CALLBACK_MANAGER(DebugMessageDispatcher, void())

//-----------------------------------------------------------------------------

static const size_t s_StackBreathingRoom = static_cast<size_t>(16 * 1024);
static size_t* const s_pMinStackLimit = reinterpret_cast<size_t*>(sizeof(size_t));

//-----------------------------------------------------------------------------

V8IsolateImpl::V8IsolateImpl(const StdString& name, const V8IsolateConstraints* pConstraints, bool enableDebugging, int debugPort):
    m_Name(name),
    m_pIsolate(Isolate::New()),
    m_DebuggingEnabled(false),
    m_DebugMessageDispatchCount(0),
    m_MaxHeapSize(0),
    m_HeapWatchLevel(0),
    m_MaxStackUsage(0),
    m_StackWatchLevel(0),
    m_pStackLimit(nullptr),
    m_IsOutOfMemory(false)
{
    V8ArrayBufferAllocator::EnsureInstalled();

    BEGIN_ADDREF_SCOPE

        m_pIsolate->SetData(0, this);

        BEGIN_ISOLATE_ENTRY_SCOPE

            V8::SetCaptureStackTraceForUncaughtExceptions(true, 64, StackTrace::kDetailed);
            if (pConstraints != nullptr)
            {
                ResourceConstraints constraints;
                constraints.set_max_new_space_size(pConstraints->GetMaxNewSpaceSize());
                constraints.set_max_old_space_size(pConstraints->GetMaxOldSpaceSize());
                constraints.set_max_executable_size(pConstraints->GetMaxExecutableSize());
                ASSERT_EVAL(SetResourceConstraints(m_pIsolate, &constraints));
            }

        END_ISOLATE_ENTRY_SCOPE

        if (enableDebugging)
        {
            BEGIN_ISOLATE_SCOPE

                EnableDebugging(debugPort);

            END_ISOLATE_SCOPE
        }

    END_ADDREF_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::AddContext(V8ContextImpl* pContextImpl, bool enableDebugging, int debugPort)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    if (!enableDebugging)
    {
        m_ContextPtrs.push_back(pContextImpl);
    }
    else
    {
        m_ContextPtrs.push_front(pContextImpl);
        EnableDebugging(debugPort);
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::RemoveContext(V8ContextImpl* pContextImpl)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    m_ContextPtrs.remove(pContextImpl);
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::EnableDebugging(int debugPort)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    if (!m_DebuggingEnabled)
    {
        if (debugPort < 1)
        {
            debugPort = 9222;
        }

        auto wrIsolate = CreateWeakRef();
        m_pDebugMessageDispatcher = CALLBACK_MANAGER(DebugMessageDispatcher)::Alloc([wrIsolate]
        {
            Concurrency::create_task([wrIsolate]
            {
                auto spIsolate = wrIsolate.GetTarget();
                if (!spIsolate.IsEmpty())
                {
                    auto pIsolateImpl = static_cast<V8IsolateImpl*>(spIsolate.GetRawPtr());
                    pIsolateImpl->DispatchDebugMessages();
                }
            });
        });

        _ASSERTE(m_pDebugMessageDispatcher);
        Debug::SetDebugMessageDispatchHandler(m_pDebugMessageDispatcher);
        ASSERT_EVAL(Debug::EnableAgent(*String::Utf8Value(CreateString(m_Name)), debugPort));

        m_DebuggingEnabled = true;
        m_DebugPort = debugPort;
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::DisableDebugging()
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    if (m_DebuggingEnabled)
    {
        Debug::DisableAgent();
        Debug::SetDebugMessageDispatchHandler(nullptr);

        if (m_pDebugMessageDispatcher != nullptr)
        {
            ASSERT_EVAL(CALLBACK_MANAGER(DebugMessageDispatcher)::Free(m_pDebugMessageDispatcher));
        }

        m_DebuggingEnabled = false;
        m_DebugMessageDispatchCount = 0;
    }
}

//-----------------------------------------------------------------------------

size_t V8IsolateImpl::GetMaxHeapSize()
{
    return m_MaxHeapSize;
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::SetMaxHeapSize(size_t value)
{
    m_MaxHeapSize = value;
    m_IsOutOfMemory = false;
}

//-----------------------------------------------------------------------------

double V8IsolateImpl::GetHeapSizeSampleInterval()
{
    return m_HeapSizeSampleInterval;
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::SetHeapSizeSampleInterval(double value)
{
    m_HeapSizeSampleInterval = value;
}

//-----------------------------------------------------------------------------

size_t V8IsolateImpl::GetMaxStackUsage()
{
    return m_MaxStackUsage;
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::SetMaxStackUsage(size_t value)
{
    m_MaxStackUsage = value;
}

//-----------------------------------------------------------------------------

V8ScriptHolder* V8IsolateImpl::Compile(const StdString& documentName, const StdString& code)
{
    BEGIN_ISOLATE_SCOPE

        SharedPtr<V8ContextImpl> spContextImpl((m_ContextPtrs.size() > 0) ? m_ContextPtrs.front() : new V8ContextImpl(this, StdString(), false, true, 0));
        return spContextImpl->Compile(documentName, code);

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::GetHeapInfo(V8IsolateHeapInfo& heapInfo)
{
    BEGIN_ISOLATE_SCOPE

    HeapStatistics heapStatistics;
    m_pIsolate->GetHeapStatistics(&heapStatistics);

    heapInfo.Set(
        heapStatistics.total_heap_size(),
        heapStatistics.total_heap_size_executable(),
        heapStatistics.total_physical_size(),
        heapStatistics.used_heap_size(),
        heapStatistics.heap_size_limit()
    );

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::CollectGarbage(bool exhaustive)
{
	BEGIN_ISOLATE_SCOPE

	
    if (exhaustive)
    {
        V8::LowMemoryNotification();
    }
    else
    {
        while (!V8::IdleNotification());
    }

    END_ISOLATE_SCOPE
}

class FileOutputStream : public v8::OutputStream {
public:
	FileOutputStream(FILE* stream) : stream_(stream) {}

	virtual int GetChunkSize() {
		return 65536;  // big chunks == faster
	}

	virtual void EndOfStream() {}

	virtual WriteResult WriteAsciiChunk(char* data, int size) {
		const size_t len = static_cast<size_t>(size);
		size_t off = 0;

		while (off < len && !feof(stream_) && !ferror(stream_))
			off += fwrite(data + off, 1, len - off, stream_);

		return off == len ? kContinue : kAbort;
	}

private:
	FILE* stream_;
};


void V8IsolateImpl::WriteHeapSnapshot(const char* filename)
{
	
	BEGIN_ISOLATE_SCOPE
		
		FILE *fp;
		fopen_s(&fp, filename, "w");
		if (fp == NULL){
			return; 
			/*
			char errorMsg[500];
			strcat_s(errorMsg, 500, "Could not open file ");
			strcat_s(errorMsg, 500, filename);
			
			throw std::exception(errorMsg);*/
		}

		HeapProfiler* profiler = m_pIsolate->GetHeapProfiler();
		Local<String> heapName = String::NewFromUtf8(m_pIsolate, "'bob'");
		const  HeapSnapshot* heapSnapShot = profiler->TakeHeapSnapshot(heapName);
		FileOutputStream stream(fp);
		heapSnapShot->Serialize(&stream, HeapSnapshot::kJSON);
		fclose(fp);

		// Work around a deficiency in the API.  The HeapSnapshot object is const
		// but we cannot call HeapProfiler::DeleteAllHeapSnapshots() because that
		// invalidates _all_ snapshots, including those created by other tools.
		const_cast<HeapSnapshot*>(heapSnapShot)->Delete();
		
	END_ISOLATE_SCOPE
}



//-----------------------------------------------------------------------------

void* V8IsolateImpl::AddRefV8Object(void* pvObject)
{
    BEGIN_ISOLATE_SCOPE

        return ::PtrFromObjectHandle(CreatePersistent(::ObjectHandleFromPtr(pvObject)));

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::ReleaseV8Object(void* pvObject)
{
    BEGIN_ISOLATE_SCOPE

        Dispose(::ObjectHandleFromPtr(pvObject));

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void* V8IsolateImpl::AddRefV8Script(void* pvScript)
{
    BEGIN_ISOLATE_SCOPE

        return ::PtrFromScriptHandle(CreatePersistent(::ScriptHandleFromPtr(pvScript)));

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::ReleaseV8Script(void* pvScript)
{
    BEGIN_ISOLATE_SCOPE

        Dispose(::ScriptHandleFromPtr(pvScript));

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void DECLSPEC_NORETURN V8IsolateImpl::ThrowOutOfMemoryException()
{
    m_IsOutOfMemory = true;
    throw V8Exception(V8Exception::Type_Fatal, m_Name, StdString(L"The V8 runtime has exceeded its memory limit"));
}

//-----------------------------------------------------------------------------

V8IsolateImpl::~V8IsolateImpl()
{
    BEGIN_ISOLATE_SCOPE

        DisableDebugging();

    END_ISOLATE_SCOPE

    m_pIsolate->Dispose();
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::OnInterrupt()
{
    std::function<void(V8IsolateImpl*)> callback; 

    BEGIN_MUTEX_SCOPE(m_InterruptMutex)

        callback = std::move(m_InterruptCallback);

    END_MUTEX_SCOPE

    if (callback)
    {
        callback(this);
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::DispatchDebugMessages()
{
    if (++m_DebugMessageDispatchCount == 1)
    {
        ProcessDebugMessages();
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::ProcessDebugMessages()
{
    m_DebugMessageDispatchCount = 0;

    BEGIN_ISOLATE_SCOPE

        if (m_ContextPtrs.size() > 0)
        {
            m_ContextPtrs.front()->ProcessDebugMessages();
        }

    END_ISOLATE_SCOPE
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::EnterExecutionScope(size_t* pStackMarker)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    // is heap size monitoring in progress?
    if (m_HeapWatchLevel == 0)
    {
        // no; there should be no heap watch timer
        _ASSERTE(m_spHeapWatchTimer.IsEmpty());

        // is a heap size limit specified?
        size_t maxHeapSize = m_MaxHeapSize;
        if (maxHeapSize > 0)
        {
            // yes; perform initial check and set up heap watch timer
            CheckHeapSize(maxHeapSize);

            // enter outermost heap size monitoring scope
            m_HeapWatchLevel = 1;
        }
    }
    else
    {
        // heap size monitoring in progress; enter nested scope
        m_HeapWatchLevel++;
    }

    // is stack usage monitoring in progress?
    if (m_StackWatchLevel == 0)
    {
        // no; there should be no stack address limit
        _ASSERTE(m_pStackLimit == nullptr);

        // is a stack usage limit specified?
        size_t maxStackUsage = m_MaxStackUsage;
        if (maxStackUsage > 0)
        {
            // yes; ensure minimum breathing room
            maxStackUsage = std::max(maxStackUsage, s_StackBreathingRoom);

            // calculate stack address limit
            size_t* pStackLimit = pStackMarker - (maxStackUsage / sizeof(size_t));
            if ((pStackLimit < s_pMinStackLimit) || (pStackLimit > pStackMarker))
            {
                // underflow; use minimum non-null stack address
                pStackLimit = s_pMinStackLimit;
            }
            else
            {
                // check stack address limit sanity
                _ASSERTE((pStackMarker - pStackLimit) >= (s_StackBreathingRoom / sizeof(size_t)));
            }

            // set and record stack address limit
            ResourceConstraints constraints;
            constraints.set_stack_limit(reinterpret_cast<uint32_t*>(pStackLimit));
            ASSERT_EVAL(SetResourceConstraints(m_pIsolate, &constraints));
            m_pStackLimit = pStackLimit;

            // enter outermost stack usage monitoring scope
            m_StackWatchLevel = 1;
        }
    }
    else
    {
        // stack usage monitoring in progress
        if ((m_pStackLimit != nullptr) && (pStackMarker < m_pStackLimit))
        {
            // stack usage limit exceeded (host-side detection)
            throw V8Exception(V8Exception::Type_General, m_Name, StdString(L"The V8 runtime has exceeded its stack usage limit"));
        }

        // enter nested stack usage monitoring scope
        m_StackWatchLevel++;
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::ExitExecutionScope()
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    // is stack usage monitoring in progress?
    if (m_StackWatchLevel > 0)
    {
        // yes; exit stack usage monitoring scope
        if (--m_StackWatchLevel == 0)
        {
            // exited outermost scope; remove stack address limit
            if (m_pStackLimit != nullptr)
            {
                // V8 has no API for removing a stack address limit
                ResourceConstraints constraints;
                constraints.set_stack_limit(reinterpret_cast<uint32_t*>(s_pMinStackLimit));
                ASSERT_EVAL(SetResourceConstraints(m_pIsolate, &constraints));
                m_pStackLimit = nullptr;
            }
        }
    }

    // is heap size monitoring in progress?
    if (m_HeapWatchLevel > 0)
    {
        // yes; exit heap size monitoring scope
        if (--m_HeapWatchLevel == 0)
        {
            // exited outermost scope; destroy heap watch timer
            m_spHeapWatchTimer.Empty();
        }
    }
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::SetUpHeapWatchTimer(size_t maxHeapSize)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    // create heap watch timer
    auto wrIsolate = CreateWeakRef();
    m_spHeapWatchTimer = new Timer(static_cast<unsigned int>(std::max(GetHeapSizeSampleInterval(), 250.0)), false, [wrIsolate, maxHeapSize] (Timer* pTimer)
    {
        // heap watch callback; is the isolate still alive?
        auto spIsolate = wrIsolate.GetTarget();
        if (!spIsolate.IsEmpty())
        {
            // yes; request callback on execution thread
            auto wrTimer = pTimer->CreateWeakRef();
            static_cast<V8IsolateImpl*>(spIsolate.GetRawPtr())->RequestInterrupt([wrTimer, maxHeapSize] (V8IsolateImpl* pIsolateImpl)
            {
                // execution thread callback; is the timer still alive?
                auto spTimer = wrTimer.GetTarget();
                if (!spTimer.IsEmpty())
                {
                    // yes; check heap size
                    pIsolateImpl->CheckHeapSize(maxHeapSize);
                }
            });
        }
    });

    // start heap watch timer
    m_spHeapWatchTimer->Start();
}

//-----------------------------------------------------------------------------

void V8IsolateImpl::CheckHeapSize(size_t maxHeapSize)
{
    _ASSERTE(m_pIsolate == Isolate::GetCurrent());
    _ASSERTE(Locker::IsLocked(m_pIsolate));

    // is the total heap size over the limit?
    V8IsolateHeapInfo heapInfo;
    GetHeapInfo(heapInfo);
    if (heapInfo.GetTotalHeapSize() > maxHeapSize)
    {
        // yes; collect garbage
        V8::LowMemoryNotification();

        // is the total heap size still over the limit?
        GetHeapInfo(heapInfo);
        if (heapInfo.GetTotalHeapSize() > maxHeapSize)
        {
            // yes; the isolate is out of memory; request script termination
            m_IsOutOfMemory = true;
            TerminateExecution();
            return;
        }
    }

    // the isolate is not out of memory; restart heap watch timer
    SetUpHeapWatchTimer(maxHeapSize);
}
