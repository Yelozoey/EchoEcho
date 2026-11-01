import unittest
from qwen3_tuning_metrics import timing

def ok(seconds): return dict(seconds=seconds,completeEos=True,strictJson=True)

class TimingTests(unittest.TestCase):
    def test_successful_nearest_rank(self):
        result=timing([ok(4),ok(1),ok(3),ok(2)])
        self.assertEqual((result['p50Seconds'],result['p95Seconds']),(2,4))
        self.assertEqual(result['failedOrIncomplete'],0)
    def test_timeout_is_not_silently_excluded(self):
        result=timing([ok(1),dict(error='timeout',wallSeconds=20.4)])
        self.assertEqual(result['timedRequests'],2);self.assertEqual(result['p95Seconds'],20.4)
        self.assertEqual(result['failedOrIncomplete'],1)
    def test_early_failure_or_invalid_measurement_not_counted_as_fast(self):
        for row in [dict(error='parse',seconds=.1),dict(seconds=float('nan')),dict(seconds=-1),dict(seconds=True),dict()]:
            self.assertEqual(timing([row])['p50Seconds'],20)
    def test_empty_not_mislabeled_zero_latency(self):
        self.assertIsNone(timing([])['p50Seconds']);self.assertEqual(timing([])['timedRequests'],0)

if __name__=='__main__':unittest.main(verbosity=2)
