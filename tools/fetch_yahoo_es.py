#!/usr/bin/env python3
"""Fetch real E-mini S&P 500 (ES) 5-minute RTH bars from Yahoo Finance and
write them as Alpha-Vantage-shaped cache files the backend importer already reads.

This is a free dev-data bootstrap: Yahoo serves only the trailing ~60 days of
5-minute data, so this is for getting real charts into the app quickly, not for
deep history (use Databento or FirstRate for that). ES = the instrument Al Brooks
teaches on (5-min, regular trading hours).

Usage:
    python tools/fetch_yahoo_es.py [OUT_DIR]

OUT_DIR defaults to backend/data/raw (gitignored). Then run:
    dotnet run --project backend -- import

Only Python stdlib is used. Notes:
- Filters to Regular Trading Hours (09:30-16:00 ET, weekdays), matching the app's
  RTH premise and Al Brooks' day-session charts.
- Drops null bars and any non-5-minute-aligned bar (Yahoo's live/partial candle).
- Output symbol is ES; files are ES-YYYY-MM.json in the AV "Time Series (5min)"
  shape, so the existing MarketDataImporter loads them with no code changes.
"""
import json
import os
import sys
import urllib.request
import datetime
import zoneinfo
import collections

YAHOO_SYMBOL = "ES=F"
OUTPUT_SYMBOL = "ES"
CHART_URL = (
    "https://query1.finance.yahoo.com/v8/finance/chart/"
    f"{YAHOO_SYMBOL}?interval=5m&range=60d"
)
ET = zoneinfo.ZoneInfo("America/New_York")
RTH_START = 9 * 60 + 30   # 09:30 ET (inclusive)
RTH_END = 16 * 60         # 16:00 ET (exclusive -> last bar opens 15:55)


def fetch_chart():
    req = urllib.request.Request(CHART_URL, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=40) as resp:
        return json.load(resp)


def is_rth(dt):
    if dt.weekday() >= 5:  # Sat/Sun
        return False
    mins = dt.hour * 60 + dt.minute
    return RTH_START <= mins < RTH_END


def is_aligned(dt):
    return dt.second == 0 and dt.minute % 5 == 0


def build_months(chart):
    res = chart["chart"]["result"][0]
    ts = res["timestamp"]
    q = res["indicators"]["quote"][0]
    vol = q.get("volume", [None] * len(ts))
    months = collections.defaultdict(dict)
    kept = skipped = 0
    for i, t in enumerate(ts):
        o, h, l, c = q["open"][i], q["high"][i], q["low"][i], q["close"][i]
        if None in (o, h, l, c):
            skipped += 1
            continue
        dt = datetime.datetime.fromtimestamp(t, ET)
        if not is_rth(dt) or not is_aligned(dt):
            skipped += 1
            continue
        key = dt.strftime("%Y-%m-%d %H:%M:%S")
        months[dt.strftime("%Y-%m")][key] = {
            "1. open": f"{o:.2f}",
            "2. high": f"{h:.2f}",
            "3. low": f"{l:.2f}",
            "4. close": f"{c:.2f}",
            "5. volume": str(int(vol[i] or 0)),
        }
        kept += 1
    return months, kept, skipped


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.join("backend", "data", "raw")
    chart = fetch_chart()
    if chart.get("chart", {}).get("error"):
        print("Yahoo error:", chart["chart"]["error"], file=sys.stderr)
        return 1
    months, kept, skipped = build_months(chart)
    os.makedirs(out_dir, exist_ok=True)
    for month, series in sorted(months.items()):
        obj = {
            "Meta Data": {
                "2. Symbol": OUTPUT_SYMBOL,
                "4. Interval": "5min",
                "Note": "source=Yahoo ES=F, RTH 09:30-16:00 ET",
            },
            "Time Series (5min)": dict(sorted(series.items())),
        }
        path = os.path.join(out_dir, f"{OUTPUT_SYMBOL}-{month}.json")
        with open(path, "w") as fh:
            json.dump(obj, fh, indent=2)
        print(f"wrote {path} ({len(series)} bars)")
    print(f"kept {kept} RTH bars, skipped {skipped}; months: {sorted(months)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
