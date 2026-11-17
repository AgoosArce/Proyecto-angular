# Diseño institucional del frontend

## Alcance y estructura

El diseño conserva los componentes standalone, las carpetas, Angular Material y las rutas actuales. No modifica contratos HTTP ni reglas de permisos. Los estilos comunes se encuentran en `TestAngular/src/styles.scss`; cada SCSS de componente contiene únicamente las particularidades de esa vista.

## Identidad visual

- Azul institucional: `--institution-primary`, #173c60.
- Acento sobrio: `--institution-accent`, #a1844c.
- Texto principal y secundario: `--institution-ink` y `--institution-muted`.
- Superficies, fondo y bordes: `--institution-surface`, `--institution-background`, `--institution-border`.
- Tipografía Roboto con alternativa Arial. Radio común: `--institution-radius`.

Reutilizar estas variables en nuevas vistas. Cambiar la identidad desde las variables comunes y evitar colores aislados en componentes.

## Patrón para nuevas pantallas

1. Contener la pantalla en `section.page`.
2. Mostrar la ubicación con `.breadcrumb` y enlaces a las rutas existentes.
3. Utilizar `header.page-heading`, `.eyebrow`, un único `h1` y una descripción breve.
4. Agrupar contenido en `.card`; usar `.module-grid` y `.module-card` para accesos relacionados.
5. Utilizar `.button` para acciones principales y `.button.secondary` para acciones secundarias. Dentro de los CRUD conservar los botones de Angular Material.
6. Mantener tablas, filtros, carga, errores, estados vacíos y paginación con los componentes Material existentes. Permitir desplazamiento horizontal de tablas en dispositivos pequeños.
7. Formularios de autenticación: `.auth-page`, etiquetas visibles, autocomplete y errores con `role="alert"`. Diálogos: título, contenido con campos outline y acciones al pie.

## Navegación y accesibilidad

El encabezado y pie son compartidos por todas las rutas. El menú conserva las condiciones de sesión y rol y señala la ruta activa. Hay enlace para saltar al contenido, foco visible y adaptación a pantallas pequeñas. Usar lenguaje formal en español, enlaces para navegar y botones para ejecutar acciones. Evitar detalles internos de implementación en los textos del sistema.

## Verificación

Ejecutar `npm run build` dentro de `TestAngular` después de modificar plantillas o estilos. Para revisión visual iniciar `npm start` y comprobar inicio, login, registro, alumnos, materias y administración en escritorio y móvil. Las pantallas protegidas y los CRUD necesitan una sesión válida y la API existente. La compilación no sustituye esa comprobación funcional.