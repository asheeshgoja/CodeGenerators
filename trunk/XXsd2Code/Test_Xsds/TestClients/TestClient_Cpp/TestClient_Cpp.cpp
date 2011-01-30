// TestClient_Cpp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "CustomerInfo.h"


int _tmain(int argc, _TCHAR* argv[])
{

	XXsd2CodeSample::CustomerOrder* co = new XXsd2CodeSample::CustomerOrder();
	co->Orders.push_back( new XXsd2CodeSample::CommonElements::OrderItem());
    co->Orders[0]->price = 100;

	return 0;
}

