EventItemBag - guia rapida
============================================================

Esta carpeta NO es leida por el GameServer (el loader ignora
subcarpetas, solo lee archivos sueltos en Data/EventItemBag/).
Es solo referencia para vos.

Cada bag real vive en Data/EventItemBag/NNN - Nombre.xml, donde NNN
es el mismo numero que su <Bag Index="NNN"> en EventItemBagManager.xml.
Los podes organizar libremente en subcarpetas (por sistema, categoria,
lo que te sirva) - el loader escanea todas las subcarpetas, a
cualquier profundidad. Solo importa el numero al inicio del nombre
del archivo; la carpeta donde este es puramente para tu organizacion.

Un archivo de bag puede tener DOS sistemas de premios:

  <Item>  formato basico   - lista simple con peso por item (DropRate)
  <Ex>    formato extendido - varios drops independientes, con
                              restriccion por clase

Cual se usa lo decide el atributo UseEx en la raiz del archivo:

    <ItemBag UseEx="0"> ... </ItemBag>   -> usa <Item> (default si no
                                             esta el atributo)
    <ItemBag UseEx="1"> ... </ItemBag>   -> usa <Ex>

Podes escribir AMBOS en el mismo archivo (uno queda sin usar, no
molesta) y cambiar entre ellos solo tocando UseEx - no hace falta
borrar nada.

Ver:
  - "01 - Formato Basico.xml"    ejemplo completo comentado de <Item>
  - "02 - Formato Extendido.xml" ejemplo completo comentado de <Ex>

Para editar un bag real, copia la parte que necesites de estos
ejemplos a Data/EventItemBag/NNN - Nombre.xml y ajusta los valores.
