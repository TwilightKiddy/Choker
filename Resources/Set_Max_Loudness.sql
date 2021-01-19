INSERT INTO servers (id, max_loudness)
  VALUES($id, $max_loudness)
  ON CONFLICT(id)
  DO UPDATE SET max_loudness=excluded.max_loudness;