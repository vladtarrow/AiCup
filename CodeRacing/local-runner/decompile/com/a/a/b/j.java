package com.a.a.b;

import java.util.Comparator;

final class j
  implements Comparator
{
  public int a(g.b paramb1, g.b paramb2)
  {
    int i = Double.compare(paramb2.b, paramb1.b);
    if (i != 0) {
      return i;
    }
    return paramb1.a.compareTo(paramb2.a);
  }
}


/* Location:              D:\Projects\AiCup\CodeRacing\local-runner\local-runner.jar!\com\a\a\b\j.class
 * Java compiler version: 7 (51.0)
 * JD-Core Version:       0.7.1
 */