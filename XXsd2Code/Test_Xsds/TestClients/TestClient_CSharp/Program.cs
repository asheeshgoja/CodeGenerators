using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            XXsd2CodeSample.CustomerOrder co = new XXsd2CodeSample.CustomerOrder();
            co.Orders.Add(new XXsd2CodeSample.CommonElements.OrderItem());
            co.Orders[0].price = 100;
        }
    }
}
