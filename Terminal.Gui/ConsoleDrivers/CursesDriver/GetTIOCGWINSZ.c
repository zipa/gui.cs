#include <stdio.h>
#include <sys/ioctl.h>

// Used to get the value of the TIOCGWINSZ variable,
// which may have different values ​​on different Unix operating systems.
//   Linux=0x005413
//   Darwin and OpenBSD=0x40087468,
//   Solaris=0x005468
// See https://stackoverflow.com/questions/16237137/what-is-termios-tiocgwinsz
int get_tiocgwinsz_value() {
    return TIOCGWINSZ;
}