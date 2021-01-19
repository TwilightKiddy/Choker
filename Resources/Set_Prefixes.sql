INSERT INTO servers (id, prefixes)
  VALUES($id, $prefixes)
  ON CONFLICT(id)
  DO UPDATE SET prefixes=excluded.prefixes;