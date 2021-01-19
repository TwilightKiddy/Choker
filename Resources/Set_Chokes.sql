INSERT INTO users (id, chokes)
  VALUES($id, $chokes)
  ON CONFLICT(id)
  DO UPDATE SET chokes=excluded.chokes;