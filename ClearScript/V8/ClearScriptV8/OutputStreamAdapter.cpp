#include "OutputStreamAdapter.h"









		

OutputStreamAdapter::OutputStreamAdapter(char*   filename) :
m_filename(filename)
{
	FILE *fp;
	fopen_s(&fp, m_filename, "w");
	fclose(fp);
}
void OutputStreamAdapter::EndOfStream() {

}

int OutputStreamAdapter::GetChunkSize() {
	return 51200;
}

v8::OutputStream::WriteResult OutputStreamAdapter::WriteAsciiChunk(char* data, int size)
{




	FILE *fp;
	fopen_s(&fp, m_filename, "a");



	fprintf(fp, data);
	fclose(fp);



	return kContinue;
}

v8::OutputStream::WriteResult OutputStreamAdapter::WriteHeapStatsChunk(v8::HeapStatsUpdate* data, int count) {

	for (int i = 0; i < count; i++)
	{
		
	}
	return kContinue;
}
