package com.google.gson.internal.bind;

import com.google.gson.Gson;
import com.google.gson.TypeAdapter;
import com.google.gson.TypeAdapterFactory;
import com.google.gson.internal..Gson.Types;
import com.google.gson.internal.ConstructorConstructor;
import com.google.gson.internal.ObjectConstructor;
import com.google.gson.reflect.TypeToken;
import com.google.gson.stream.JsonReader;
import com.google.gson.stream.JsonToken;
import com.google.gson.stream.JsonWriter;
import java.io.IOException;
import java.lang.reflect.Type;
import java.util.Collection;
import java.util.Iterator;

public final class CollectionTypeAdapterFactory
  implements TypeAdapterFactory
{
  private final ConstructorConstructor constructorConstructor;
  
  public CollectionTypeAdapterFactory(ConstructorConstructor paramConstructorConstructor)
  {
    this.constructorConstructor = paramConstructorConstructor;
  }
  
  public TypeAdapter create(Gson paramGson, TypeToken paramTypeToken)
  {
    Type localType1 = paramTypeToken.getType();
    Class localClass = paramTypeToken.getRawType();
    if (!Collection.class.isAssignableFrom(localClass)) {
      return null;
    }
    Type localType2 = .Gson.Types.getCollectionElementType(localType1, localClass);
    TypeAdapter localTypeAdapter = paramGson.getAdapter(TypeToken.get(localType2));
    ObjectConstructor localObjectConstructor = this.constructorConstructor.get(paramTypeToken);
    Adapter localAdapter = new Adapter(paramGson, localType2, localTypeAdapter, localObjectConstructor);
    return localAdapter;
  }
  
  private static final class Adapter
    extends TypeAdapter
  {
    private final TypeAdapter elementTypeAdapter;
    private final ObjectConstructor constructor;
    
    public Adapter(Gson paramGson, Type paramType, TypeAdapter paramTypeAdapter, ObjectConstructor paramObjectConstructor)
    {
      this.elementTypeAdapter = new TypeAdapterRuntimeTypeWrapper(paramGson, paramTypeAdapter, paramType);
      this.constructor = paramObjectConstructor;
    }
    
    public Collection read(JsonReader paramJsonReader)
      throws IOException
    {
      if (paramJsonReader.peek() == JsonToken.NULL)
      {
        paramJsonReader.nextNull();
        return null;
      }
      Collection localCollection = (Collection)this.constructor.construct();
      paramJsonReader.beginArray();
      while (paramJsonReader.hasNext())
      {
        Object localObject = this.elementTypeAdapter.read(paramJsonReader);
        localCollection.add(localObject);
      }
      paramJsonReader.endArray();
      return localCollection;
    }
    
    public void write(JsonWriter paramJsonWriter, Collection paramCollection)
      throws IOException
    {
      if (paramCollection == null)
      {
        paramJsonWriter.nullValue();
        return;
      }
      paramJsonWriter.beginArray();
      Iterator localIterator = paramCollection.iterator();
      while (localIterator.hasNext())
      {
        Object localObject = localIterator.next();
        this.elementTypeAdapter.write(paramJsonWriter, localObject);
      }
      paramJsonWriter.endArray();
    }
  }
}


/* Location:              D:\Projects\AiCup\CodeRacing\local-runner\local-runner.jar!\com\google\gson\internal\bind\CollectionTypeAdapterFactory.class
 * Java compiler version: 5 (49.0)
 * JD-Core Version:       0.7.1
 */