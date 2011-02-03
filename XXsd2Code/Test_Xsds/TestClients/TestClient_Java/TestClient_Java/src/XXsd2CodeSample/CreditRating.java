package	XXsd2CodeSample;
 
//enumeration	CreditRating
public enum	CreditRating
{
	Good("700"),
	VeryGood("750"),
	ExtremelyGood("790"),
	Poor("300")
			
	;
	private final String value;

	//private constructor
	CreditRating(String value) 
	{	   this.value = value;}

	//fromValue
	public static CreditRating fromValue(String value) 
	 {   
	   if (value != null) 
	   {   
	     for (CreditRating v : values()) 
	     {   
	       if (v.value.equals(value)) 
	       {   
	         return v;   
	       }   
	     }   
	   }   
	   return null;   
	 } 

	//toString
	@Override
	public String toString() {   return value;}   
}
