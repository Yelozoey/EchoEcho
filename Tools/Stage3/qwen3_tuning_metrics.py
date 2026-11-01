"""Candidate timing includes failed requests; a failure cannot improve a percentile."""
import math

def percentile(values, fraction):
    return sorted(values)[math.ceil(len(values)*fraction)-1] if values else None

def timing(rows):
    values=[]; failures=0
    for row in rows:
        seconds=row.get('seconds')
        valid=isinstance(seconds,(int,float)) and not isinstance(seconds,bool) and math.isfinite(seconds) and seconds>=0
        complete=row.get('completeEos') is True and row.get('strictJson') is True and not row.get('error')
        if not valid or not complete:
            failures+=1
            wall=row.get('wallSeconds')
            seconds=max(20.0,wall) if isinstance(wall,(int,float)) and math.isfinite(wall) else 20.0
        values.append(seconds)
    return dict(timedRequests=len(values),failedOrIncomplete=failures,
                failurePenaltyFloorSeconds=20,p50Seconds=percentile(values,.5),p95Seconds=percentile(values,.95))
