#include <stdio.h>
#include <sys/ioctl.h>

// This function is used to get the value of the TIOCGWINSZ variable,
// which may have different values ​​on different Unix operating systems.
// In Linux=0x005413, in Darwin and OpenBSD=0x40087468,
// In Solaris=0x005468
// The best solution is having a function that get the real value of the current OS
int get_tiocgwinsz_value() {
    return TIOCGWINSZ;
}