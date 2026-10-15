import time


class Clock:
    def now(self):
        if hasattr(time, "ticks_ms"):
            return time.ticks_ms()
        return int(time.monotonic() * 1000)

    def elapsed(self, now, before):
        if hasattr(time, "ticks_diff"):
            return time.ticks_diff(now, before)
        return now - before

    def utc(self):
        t = time.gmtime()
        if t[0] < 2024:
            raise OSError("RTC UTC no sincronizado")
        return "%04d-%02d-%02dT%02d:%02d:%02dZ" % t[:6]
