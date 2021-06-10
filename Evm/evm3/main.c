#include <reg51.h>

int mul(char a, char b)
{
	int r;
	char m, i, j;
	int s = 0;
	
	m = 0x80; // m = 1000 0000

	for(i = 1; i <= 8; i++) 
	{
		r = 0; // r = A*Bi*2^(-i)
		if ((b & m) != 0) // if Bi = 1
		{ 
			r = a;
			r <<= 8;
			for (j = 0; j < i; j++) 
			{
				r = (r >> 1) & 0x7FFF; // R = A*2^(i)
			}
		}
		
		s = s + r; // Si+1 = Si + A*Bi*2^(-i)
		m = (m >> 1) & 0x7F;
	}
	return s;
}

void main() 
{
	int s;
	char a, b;
	a = 0x18;
	b = 0x60;
	s = mul(a, b);
	while(1);
}
