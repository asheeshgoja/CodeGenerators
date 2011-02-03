// TestClient_CppCli.cpp : main project file.

#include "stdafx.h"
#include "CustomerInfo.h"

using namespace System;

int main(array<System::String ^> ^args)
{
	XXsd2CodeSample::CustomerOrder^ co = gcnew XXsd2CodeSample::CustomerOrder();
	co->Orders->Add(gcnew XXsd2CodeSample::CommonElements::OrderItem());
	co->Rating = XXsd2CodeSample::CreditRating::ExtremelyGood;
    co->Orders[0]->price = 100;

    return 0;
}
