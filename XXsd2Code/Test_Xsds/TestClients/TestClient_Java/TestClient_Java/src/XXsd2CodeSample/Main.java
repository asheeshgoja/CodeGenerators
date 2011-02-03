package XXsd2CodeSample;

import XXsd2CodeSample.CommonElements.OrderItem;


public class Main {

	/**
	 * @param args
	 */
	public static void main(String[] args) {
		// TODO Auto-generated method stub
		CustomerOrder co = new CustomerOrder();
        co.Orders.add(new OrderItem());
        co.Orders.get(0).price = 100;	
	}
}
