// Same v10 rules, updated trusted boundary examples; explicit candidate only.
using System;
using System.Collections.Generic;
using EchoStation.AI.Tokenization;
namespace EchoStation.AI.Context
{
 public static class Qwen3V11Prompt
 {
  public const string Text=Qwen3V10Prompt.Text;
  public const string Sha256=Qwen3V10Prompt.Sha256;
  public const string ExamplesSha256="44f13034b891fd2a5eed70de0e19fbcaf33a344925ee59e567098310a19e6c56";
  public static IReadOnlyList<Qwen3ChatExample> Examples {get;}=Array.AsReadOnly(new Qwen3ChatExample[]{
   new Qwen3ChatExample("随我移动，一直跟在玩家身后。","{\"intent\":\"follow\"}"),
   new Qwen3ChatExample("我要前去查看，你别随我走，在这里等候。","{\"intent\":\"wait\"}"),
   new Qwen3ChatExample("调查示例仪器的信号，只扫描而不操作。","{\"intent\":\"investigate\",\"targetName\":\"sample_scan_01\",\"content\":\"我前去扫描。\"}"),
   new Qwen3ChatExample("将示例危险装置列为警戒对象并提醒我。","{\"intent\":\"warn\",\"dangerTarget\":\"sample_danger_01\",\"content\":\"请与危险设备保持距离。\"}"),
   new Qwen3ChatExample("这个已知定位标记是指向哪个地方？","{\"intent\":\"answer\",\"content\":\"我只能根据已发现的定位记录回答。\"}"),
   new Qwen3ChatExample("这道机关由你来完成，直接把供电切换好。","{\"intent\":\"refuse\",\"content\":\"我不能代为操作机关或解谜。\"}"),
   new Qwen3ChatExample("请整理已发现的记录信息。","{\"intent\":\"answer\",\"content\":\"我会依据已经归档的摘要整理。\"}"),
   new Qwen3ChatExample("门和维护终端都由你操作，输入代码再打开门。","{\"intent\":\"refuse\",\"content\":\"门和终端必须由玩家操作，我不能代为执行。\"}")
  });
 }
}
