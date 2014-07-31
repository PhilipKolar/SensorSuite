#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>

int main()
{
    float f = 1.62;
    while (1)
    {
        f += 1 / f;
        printf("%f\n", f);
        sleep(1);
    }

    return 0;
}