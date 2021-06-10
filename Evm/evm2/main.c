#include <reg51.h>

#define LENGTH 17

char code  input[LENGTH+1] = "This programmator";
char xdata output[LENGTH+1];

char i, j, temp;

int main() 
{
	for (i = 0; i < LENGTH + 1; i++)
	{
    	*(output + i) = *(input + i);
	}
	for (i = 0; i < LENGTH - 1; i++)
	{
		for (j = i + 1; j <= LENGTH - 1; j++)
		{
			if (*(output + j) < *(output + i))
			{
				temp = *(output + i);
				*(output + i) = *(output + j);
				*(output + j) = temp;
			}
		}
	}
	return 0;
}